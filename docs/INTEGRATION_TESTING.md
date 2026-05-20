# Integration Testing Guide

How to write integration tests for the OptiLifts Core API.

---

## The Pattern

Every integration test file follows this structure:

```
MyFeatureFixture - starts the container, migrates, starts the app once
MyFeatureIntegrationTests - test class, receives the fixture
```

---

## The Rules

### 1. Use `IClassFixture<T>`, not `IAsyncLifetime` on the test class

`IClassFixture<T>` creates the fixture once per test class and shares it across all tests in that class. `IAsyncLifetime` on the test class itself runs `InitializeAsync` once per test method - that means a new Postgres container and a full web host startup for every single test.

This is purely an efficiency concern. The race condition protection comes from rule 2, not from this.

```csharp
// correct - one container for all tests in the class
public sealed class MyFixture : IAsyncLifetime { ... }
public class MyTests : IClassFixture<MyFixture>
{
    public MyTests(MyFixture fixture) { _fixture = fixture; }
}

// wrong - new container per test method
public class MyTests : IAsyncLifetime { ... }
```

---

### 2. Use `UseSetting`, never `Environment.SetEnvironmentVariable`

Environment variables are process-global. xUnit runs test classes in parallel, so two fixtures setting `POSTGRES_CONNECTION_STRING` at the same time clobber each other. Whichever writes last wins, and the other fixture ends up pointing at the wrong database.

`UseSetting` injects config directly into the host before `Program.cs` runs, and is scoped to that specific `WebApplicationFactory` instance only. This is why `Program.cs` reads from `builder.Configuration` instead of `Environment.GetEnvironmentVariable` - so that this override actually takes effect.

`ConfigureAppConfiguration` looks like it should work but doesn't with the minimal hosting model (`WebApplication.CreateBuilder`). By the time its sources are added, `Program.cs` has already read from `builder.Configuration`. `UseSetting` avoids this problem entirely.

```csharp
// correct - injected into host config before Program.cs runs, scoped to this factory only
_factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
{
    builder.UseEnvironment("Testing");
    builder.UseSetting("POSTGRES_CONNECTION_STRING", _postgres.GetConnectionString() + ";Pooling=false");
    builder.UseSetting("JWT_SECRET", JwtSecret);
    builder.UseSetting("JWT_EXP_MINUTES", "60");
    builder.UseSetting("FRONTEND_ORIGIN", "localhost:5173");
    builder.UseSetting("RUN_MIGRATIONS", "false");
});

// wrong - writes to the process, races with every other parallel fixture
Environment.SetEnvironmentVariable("POSTGRES_CONNECTION_STRING", _postgres.GetConnectionString());
```

Note the `Pooling=false` appended to the connection string. Npgsql's connection pool is global at the process level. When multiple factories run in parallel and one disposes, it can corrupt open connections belonging to another factory still running. `Pooling=false` disables pooling so each connection is independent and factory teardown cannot affect other factories.

---

### 3. Always set `RUN_MIGRATIONS=false`

`Program.cs` runs migrations on startup unconditionally unless you set this flag. If your fixture has already applied migrations before the factory starts (which it should - see rule 4), the app will try to re-create PostgreSQL enum types that already exist and throw:

`23505: duplicate key value violates unique constraint "pg_type_typname_nsp_index"`

`RUN_MIGRATIONS=false` is safe because your fixture handles migrations itself before the factory ever starts.

---

### 4. Migrate directly against the container, before the factory starts

Do not run migrations through `_factory.Services`. That starts the entire web host just to get a `DbContext` - migrations should be done before the app is even created. Build a `DbContext` directly from the connection string instead.

If migrations fail this way, the factory never starts and the error is immediately obvious. If you migrate through the factory, the error gets buried in host startup noise.

```csharp
public async Task InitializeAsync()
{
    await _postgres.StartAsync();

    // migrate directly - no web host involved
    var dbOptions = new DbContextOptionsBuilder<OptiLiftsDbContext>()
        .UseNpgsql(_postgres.GetConnectionString())
        .Options;

    await using (var db = new OptiLiftsDbContext(dbOptions))
    {
        await db.Database.MigrateAsync();
        await OptiLifts.Infrastructure.Database.Seeders.DatabaseSeeder.SeedAsync(db);
    }

    // now start the factory - database is ready, app just serves HTTP
    _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        ...
    });
}
```

---

### 5. Each test gets its own `HttpClient`

`HttpClient.DefaultRequestHeaders` is mutable state on the client instance. If tests share a client, one test setting `Authorization = null` or changing the bearer token bleeds into the next test. Always call `_fixture.CreateClient()` inside each test method.

```csharp
// correct - fresh client, no shared state
[Fact]
public async Task MyTest()
{
    var client = _fixture.CreateClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
}

// wrong - headers from previous test are still set
private readonly HttpClient _client; // shared across all tests
```

The `CreateClient` call should also set these options:

```csharp
public HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
{
    BaseAddress = new Uri("https://localhost"),  // matches UseHttpsRedirection in Program.cs
    AllowAutoRedirect = false                    // redirects show up as 3xx, not silent follows
});
```

Without `BaseAddress = https`, every request hits `UseHttpsRedirection` and silently follows the redirect. Without `AllowAutoRedirect = false`, a bug that returns a 301 looks like a pass instead of a failure.

---

### 6. Use a unique database name per fixture

Each fixture gets its own container, so names rarely collide in practice. But using a unique GUID name guarantees no two fixtures ever share state even if containers are reused by the test runner.

The GUID must be set in the constructor, not as a field initializer, because `_postgres` depends on it and C# does not allow instance fields to reference other instance fields during initialization.

```csharp
private readonly string _dbName = $"optilifts_tests_{Guid.NewGuid():N}";
private readonly PostgreSqlContainer _postgres;

public MyFixture()
{
    _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase(_dbName)
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();
}
```

---

### 7. Use `await using` and `CreateAsyncScope` when seeding

`DbContext` implements `IAsyncDisposable`. Using synchronous `using` disposes it on a thread pool thread which can cause warnings and leave connections open longer than needed. Always use `await using` with `CreateAsyncScope`.

```csharp
// correct
await using var scope = _factory.Services.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();

// wrong
using var scope = _factory.Services.CreateScope();
```

---

## Full minimal example

```csharp
public sealed class MyApiFixture : IAsyncLifetime
{
    public const string JwtSecret = "integration-test-secret-integration-test-secret";
    private readonly string _dbName = $"optilifts_tests_{Guid.NewGuid():N}";
    private readonly PostgreSqlContainer _postgres;
    private WebApplicationFactory<Program> _factory = null!;

    public MyApiFixture()
    {
        _postgres = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase(_dbName)
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var dbOptions = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var db = new OptiLiftsDbContext(dbOptions))
        {
            await db.Database.MigrateAsync();
            await OptiLifts.Infrastructure.Database.Seeders.DatabaseSeeder.SeedAsync(db);
        }

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("POSTGRES_CONNECTION_STRING", _postgres.GetConnectionString() + ";Pooling=false");
            builder.UseSetting("JWT_SECRET", JwtSecret);
            builder.UseSetting("JWT_EXP_MINUTES", "60");
            builder.UseSetting("FRONTEND_ORIGIN", "localhost:5173");
            builder.UseSetting("RUN_MIGRATIONS", "false");
        });
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false
    });

    public HttpClient CreateAuthenticatedClient(User user)
    {
        var client = CreateClient();
        var tokenService = new JwtTokenService(JwtSecret, 60);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenService.CreateToken(user));
        return client;
    }

    public async Task<Guid> SeedUserAsync(string email)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, Email = email, DisplayName = "Test User", PasswordHash = "integration-hash" });
        await db.SaveChangesAsync();
        return userId;
    }
}

public class MyIntegrationTests : IClassFixture<MyApiFixture>
{
    private readonly MyApiFixture _fixture;
    public MyIntegrationTests(MyApiFixture fixture) { _fixture = fixture; }

    [Fact]
    public async Task MyEndpoint_Returns200()
    {
        var userId = await _fixture.SeedUserAsync("test@optilifts.com");
        var client = _fixture.CreateAuthenticatedClient(new User { Id = userId });

        var response = await client.GetAsync("/api/my-endpoint");
        response.EnsureSuccessStatusCode();
    }
}
```
