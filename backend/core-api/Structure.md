# OptiLifts Core API Architecture

This backend is structured using **Domain-Driven Design (DDD)** and **Logical CQRS** (Command Query Responsibility Segregation)

## 4 Layers 

Initial core backend is divided into 4 layers and **inner layers cannot reference outer layers.**

1. **`OptiLifts.Domain` (The Core):** 
   * Contains our absolute business rules, Entities (like `User`, `WorkoutSession`), and Enums.
   * *Rule:* No external dependencies - database code,  JSON, external APIs.

2. **`OptiLifts.Application` (The Use Cases):**
   * Contains the actual features of our app (e.g., `CreateSession`, `GetActiveSession`).
   * *Rule:* It only knows about the Domain. It orchestrates logic but leaves database saving to the Infrastructure layer.

3. **`OptiLifts.Infrastructure` (The Tech):**
   * Where the code meets the outside world. Contains Entity Framework, PostgreSQL configurations, and Redis caching.

4. **`OptiLifts.API` (The Entry Point):**
   * The web server. Contains our Controllers and `Program.cs`. It is extremely thin and just passes HTTP requests down to the Application layer.

---

## Logical CQRS & Feature Folders

Inside the `Application` layer, we separate our Read operations (Queries) from our Write operations (Commands) using a library called **MediatR**.

Instead of grouping files by type (e.g., all commands in one folder, all handlers in another), we use **Feature Folders**. Everything related to a single action lives together.

### Example Folder Structure
```text
OptiLifts.Application/
└── Workouts/
    ├── CreateSession/               <-- A "Write" Feature
    │   ├── CreateSessionCommand.cs
    │   └── CreateSessionHandler.cs
    └── GetActiveSession/            <-- A "Read" Feature
        ├── GetActiveSessionQuery.cs
        └── GetActiveSessionHandler.cs