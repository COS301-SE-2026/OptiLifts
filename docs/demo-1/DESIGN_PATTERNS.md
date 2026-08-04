# Design Patterns

This document covers the design patterns applied in OptiLifts, grouped by the GoF (Gang of Four) categories: Creational, Structural, and Behavioural. Patterns planned for future implementation are listed separately.

---

## Currently Used

### Creational Patterns

#### Builder

Where: EF Core entity configurations in `Infrastructure/Database/Configurations/`.
Area: Backend - Database layer

The Builder pattern is applied through EF Core's `EntityTypeBuilder<T>` API, which constructs complex entity mapping objects incrementally through a sequence of method calls. Each call configures a single aspect of the entity, and the final mapping is only resolved when EF Core builds the model.

```csharp
builder.HasKey(u => u.Id);
builder.Property(u => u.Email).IsRequired().HasMaxLength(255);
builder.HasIndex(u => u.Email).IsUnique();
```

---

#### Singleton

Where: `IJwtTokenService` in `Program.cs`.
Area: Backend - Authentication

```csharp
builder.Services.AddSingleton<IJwtTokenService>(_ => new JwtTokenService(jwtSecret, jwtExpiryMinutes));
```

A single instance of `JwtTokenService` is created for the lifetime of the application and shared across all consumers. This is appropriate because the service holds only the JWT secret and expiry duration, which are immutable at runtime and carry no per-request state.

---

### Structural Patterns

#### Facade

Where: The controllers (`WorkoutsController`, `AuthController`, `ExercisesController`).
Area: Backend - API layer

Each controller acts as a facade, exposing a simplified HTTP interface over the underlying MediatR pipeline, handler logic, and persistence layer. The HTTP client interacts only with the endpoint and has no visibility into internal processing.

```csharp
[HttpPost]
public async Task<IActionResult> CreateWorkout(CreateWorkoutRequest request)
{
    var result = await _mediator.Send(command);
    return Created(..., result);
}
```

---

#### Proxy

Where: EF Core's `DbContext` (`OptiLiftsDbContext`).
Area: Backend - Database layer

`DbSet<T>` acts as a proxy for the underlying database. It presents a collection-like interface while transparently translating LINQ expressions into SQL, managing connections, tracking entity changes, and handling transactions. The rest of the application interacts with the proxy without direct knowledge of the persistence mechanism.

---

### Behavioural Patterns

#### Strategy

Where: `IPasswordHasher` in `Application/Auth/Abstractions/`.
Area: Backend - Authentication

```csharp
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string hash, string password);
}
```

The hashing algorithm is encapsulated behind an interface, allowing the implementation to be substituted at the dependency injection registration point. Handlers depend on `IPasswordHasher`, not on `BcryptPasswordHasher` directly. Replacing the algorithm requires only a new implementing class and a single change to the DI configuration.

---

#### Mediator

Where: MediatR. All commands and queries flow through `IMediator`.
Area: Backend - Application layer

```csharp
// Controller dispatches without knowledge of the handler
var result = await _mediator.Send(new CreateWorkoutCommand(...));

// Handler processes without knowledge of the caller
public class CreateWorkoutHandler : IRequestHandler<CreateWorkoutCommand, CreateWorkoutResult>
```

The `IMediator` object encapsulates all inter-object communication. Controllers and handlers have no direct references to each other. Adding a new feature requires only a new command and handler pair, with no modification to existing components.

---

#### Observer

Where: React's `AuthContext` with `useAuth()`.
Area: Frontend - Auth state management

```tsx
// AuthProvider holds the subject state
const [session, setSession] = React.useState(...)

// Consumers subscribe via the hook
const { isAuthenticated, user } = useAuth()
```

`AuthProvider` acts as the subject. Any component that calls `useAuth()` subscribes to the authentication state. When the session changes, React automatically re-renders all subscribed components without requiring polling or manual state checks.

---

## Patterns to Adopt

#### Decorator

Area: Backend - Application layer
Problem: Logging and validation are required across all handlers. Embedding these concerns individually within each handler results in inconsistent coverage and duplicated code.

```csharp
public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, ...)
    {
        _logger.LogInformation("Handling {Request}", typeof(TRequest).Name);
        var response = await next();
        _logger.LogInformation("Handled {Request}", typeof(TRequest).Name);
        return response;
    }
}
```

The behaviour wraps the handler without modifying it. Registered once in `Program.cs`, it executes before and after every handler in the pipeline automatically.

---

#### Template Method

Area: Backend - Application layer
Problem: The majority of handlers share an identical structure: validate input, fetch data from the database, map to a DTO, and return. Only the fetch and mapping steps vary between implementations.

```csharp
public abstract class BaseQueryHandler<TQuery, TResult>
{
    public async Task<TResult> Handle(TQuery request, CancellationToken ct)
    {
        Validate(request);
        var data = await FetchData(request, ct);
        return Map(data);
    }

    protected abstract Task<TData> FetchData(TQuery request, CancellationToken ct);
    protected abstract TResult Map(TData data);
}
```

The base class defines the algorithm skeleton. Subclasses implement only the steps specific to their context.

---

#### Chain of Responsibility

Area: Backend - API layer
Problem: Authorisation checks, ownership validation, and input rules are currently scattered across handlers and expressed as inline exceptions. As the number of rules grows, this approach becomes difficult to maintain and extend.

```csharp
public class AuthorisationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, ...)
    {
        if (request is IRequiresAuthorisation r)
            await _authService.AssertAuthorised(r);

        return await next();
    }
}
```

Each behaviour in the MediatR pipeline represents a link in the chain. Requests pass through each link in sequence. Any link may reject the request or forward it to the next. Checks can be added, removed, or reordered without modifying the handlers themselves.

---

#### State

Area: Backend - Domain layer
Problem: An active workout session transitions through distinct states: idle, active, resting, and completed. Managing this with boolean flags and conditional branches produces code that is difficult to extend and reason about as the number of states increases.

```csharp
public abstract class SessionState
{
    public abstract void CompleteSet(WorkoutSession session);
    public abstract void Finish(WorkoutSession session);
}

public class ActiveState : SessionState
{
    public override void CompleteSet(WorkoutSession session)
    {
        session.LogSet();
        session.TransitionTo(new RestState());
    }
}
```

Each state class encapsulates the behaviour and valid transitions for that state. The session delegates all action handling to its current state, and transitions are explicit and controlled.

When to add: When the active session screen is implemented.
