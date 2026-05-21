# Architecture

## Overview

The application follows **Clean Architecture** — dependencies flow strictly inward. The domain has no knowledge of infrastructure; infrastructure has no knowledge of the API layer.

```
┌──────────────────────────────────────┐
│  TaskApp.Api                         │
│  Azure Functions, Middleware, OpenAPI│
└────────────────┬─────────────────────┘
                 │ depends on
┌────────────────▼─────────────────────┐
│  TaskApp.Application                 │
│  Interfaces, DTOs, Factories, Result │
└────────────────┬─────────────────────┘
                 │ depends on
┌────────────────▼─────────────────────┐
│  TaskApp.Domain                      │
│  Entities, Enums, BaseEntity         │
│  No external dependencies            │
└──────────────────────────────────────┘
                 ▲
┌────────────────┴─────────────────────┐
│  TaskApp.Infrastructure              │
│  Repository, EF Core, Services       │
│  Implements Application interfaces   │
└──────────────────────────────────────┘
```

Infrastructure depends on Application (to implement its interfaces) and on Domain (for entities). It does NOT depend on the API layer.

---

## Design Patterns

### Repository Pattern — `IRepository<T>`

Generic repository with a rich query API. Business logic never touches `DbSet` or EF Core directly.

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<T?> GetByIdAsync(Guid id, Func<IQueryable<T>, IQueryable<T>> include, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAllAsync(Func<IQueryable<T>, IQueryable<T>> include, CancellationToken ct = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>>? predicate, Func<IQueryable<T>, IQueryable<T>> include, CancellationToken ct = default);
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
}
```

`DeleteAsync` performs a **soft delete** — sets `IsDeleted = true` and calls `_dbSet.Update`. The record is never physically removed.

---

### Unit of Work — `IUnitOfWork`

All mutations go through a single `SaveChangesAsync` call, ensuring atomicity.

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

Services call `repository.AddAsync / UpdateAsync / DeleteAsync` (which stage changes in the EF change tracker), then `unitOfWork.SaveChangesAsync` to commit once.

---

### Factory Pattern — `ITaskFactory`

Entity construction is centralized. `TaskService` never calls `new Task { ... }` directly.

```csharp
public interface ITaskFactory
{
    Task Create(CreateTaskDto dto);
    Task ApplyUpdate(Task existing, UpdateTaskDto dto);
}
```

`Create` sets default status to `Pending` and defaults `AssignedTo` to the creator when not specified. `ApplyUpdate` mutates the entity in place and returns it.

---

### Result Pattern — `Result<T>` / `Result`

Services never throw business exceptions. Every operation returns a `Result` that callers must inspect.

```csharp
// Success path
return Result<TaskDto>.Success(ToDto(task));

// Failure path
return Result<TaskDto>.Failure("Task not found");

// In the Function:
if (!result.IsSuccess)
{
    var response = req.CreateResponse(HttpStatusCode.NotFound);
    await response.WriteAsJsonAsync(new { error = result.Message });
    return response;
}
```

---

### Soft Delete

`BaseEntity` carries an `IsDeleted` flag and a `MarkAsDeleted()` method. `Repository.DeleteAsync` calls `MarkAsDeleted()` and stages an update — no SQL `DELETE` is ever issued. Queries in `TaskService` filter on `CreatedBy == userId || AssignedTo == userId`, which implicitly excludes deleted records because EF applies the soft-delete filter at the `DbSet` level in production (configurable via global query filters).

---

## Authentication

Two mechanisms are supported, both issuing the same app JWT on success.

### Email / Password

```
POST /auth/register  →  BCrypt.HashPassword  →  Save User  →  Issue JWT
POST /auth/login     →  BCrypt.Verify        →  Issue JWT
```

### Google OAuth

```
Browser                     Backend
──────                      ───────
1. Google popup (useGoogleLogin)
2. Google returns id_token
3. POST /auth/google { idToken }
                            4. GoogleJsonWebSignature.ValidateAsync
                            5. Find by GoogleId → found? → Issue JWT
                               Not found? → Find by Email
                                 Found? → Link GoogleId, Issue JWT
                                 Not found? → Create user, Issue JWT
4. Store app JWT in localStorage
5. All subsequent requests use app JWT
```

The backend uses `Google.Apis.Auth` — no Azure AD, no MSAL, no tenant configuration required.

### JWT Structure

```csharp
Claims:
  ClaimTypes.NameIdentifier  →  user.Id (Guid)
  ClaimTypes.Email           →  user.Email
  ClaimTypes.Name            →  user.FullName

Expiry:   7 days
Algorithm: HMAC-SHA256
```

`AuthMiddleware.ValidateRequest` extracts the Bearer token from the `Authorization` header and calls `JwtService.ValidateToken`. On failure it returns `null` and the function responds `401`.

---

## Data Flow — Create Task

```
1. POST /api/tasks  (HttpTrigger, Anonymous)
2. AuthMiddleware.ValidateRequest  →  extract userId from JWT
3. Deserialize body  →  CreateTaskDto
4. Validate: title not empty
5. dto with { CreatedBy = userId }
6. TaskService.CreateAsync
7.   taskFactory.Create(dto)          ← Factory Pattern
8.   repository.AddAsync(task)
9.   unitOfWork.SaveChangesAsync()
10.  repository.GetByIdAsync(task.Id, includes)   ← reload with nav props
11.  return Result<TaskDto>.Success(ToDto(task))
12. Function writes 201 + body
```

---

## Database

### Development

EF Core InMemory provider — zero setup, isolated per test run via `Guid.NewGuid()` database name.

### Production-Ready

`ApplicationDbContext` is configured for SQL Server. `TaskConfiguration.cs` defines the EF Core entity mapping. Migration assembly is set to `TaskApp.Infrastructure`.

```csharp
options.UseSqlServer(
    connectionString,
    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
```

SQL scripts in `/sql/` cover schema creation, stored procedures, and seed data.

### Entity Relationship

```
Users ──< Tasks (CreatedBy FK)
Users ──< Tasks (AssignedTo FK, nullable)
```

`BaseEntity` fields on every entity: `Id`, `CreatedAt`, `UpdatedAt`, `CreatedBy (Guid?)`, `UpdatedBy (Guid?)`, `IsDeleted`.

---

## Frontend Architecture

### Provider Tree

```
<BrowserRouter>
  <AuthProvider>          ← JWT state, login/register/logout
    <TaskProvider>        ← Task state + 30s polling
      <Routes>
        /login            → LoginPage
        /register         → RegisterPage
        / (protected)     → Dashboard
        *                 → NotFoundPage
      </Routes>
    </TaskProvider>
  </AuthProvider>
</BrowserRouter>
```

### TaskContext — Polling Strategy

`TaskProvider` starts a 30-second polling interval when the user is authenticated. On login/logout it starts/stops automatically via `useEffect` on `isAuthenticated`.

```typescript
// Initial load — shows spinner
fetchTasks(showLoading: true)

// Poll — silent background refresh
setInterval(() => fetchTasks(showLoading: false), 30_000)
```

Mutations (`createTask`, `updateTask`, `deleteTask`) call the service then immediately re-fetch, so the UI is always consistent with the server.

### Component Hierarchy

```
Dashboard
├── Layout (Navbar)
├── TaskFilters         ← local filter state (searchTerm, statusFilter)
├── TaskCard[]          ← reads from TaskContext.tasks, filtered client-side
├── Modal
│   └── TaskForm        ← calls TaskContext.createTask / updateTask
└── error banner        ← reads TaskContext.error
```

Local filtering in Dashboard is instantaneous (no round-trip). Server-side filtering via `SearchAsync` is triggered only when the user explicitly passes query parameters to `GET /tasks`.

### Path Aliases

Configured in both `vite.config.ts` (for bundler) and `tsconfig.app.json` (for TypeScript):

| Alias | Resolves to |
|-------|-------------|
| `@/*` | `src/*` |
| `@components/*` | `src/components/*` |
| `@services/*` | `src/services/*` |
| `@assets/*` | `src/assets/*` |

---

## CI/CD

Two GitHub Actions workflows trigger on push/PR to `main` or `develop`.

### `api.yml`

```
paths: api/**, tests/**

jobs:
  build-and-test:
    1. actions/setup-dotnet@v4  (.NET 10)
    2. dotnet restore api/TaskApp.sln
    3. dotnet build --configuration Release
    4. dotnet test --configuration Release
```

### `client.yml`

```
paths: client/**

jobs:
  build:
    1. actions/setup-node@v4  (Node 22)
    2. pnpm/action-setup@v4
    3. pnpm install --frozen-lockfile
    4. pnpm exec tsc --noEmit       ← type check
    5. pnpm build                   ← production bundle
```

---

## Testing Strategy

| Layer | Approach |
|-------|----------|
| Domain | Covered implicitly via service tests |
| Application (Services) | xUnit + EF InMemory — real DB behavior, no mocks for repository |
| Auth flow | xUnit + `Mock<IGoogleAuthService>` — Google token validation is the only external dependency mocked |
| JWT | Unit tests — no database, pure logic |

InMemory database is preferred over mocking the repository because it validates actual EF Core query behavior (includes, filters, ordering) without requiring a running SQL Server.

---

## Security

| Concern | Mechanism |
|---------|-----------|
| Password storage | BCrypt with per-password salt |
| Token authentication | HMAC-SHA256 JWT, validated on every request |
| Google token validation | `GoogleJsonWebSignature.ValidateAsync` — cryptographic, no secrets needed |
| Authorization | Ownership check in service layer (`CreatedBy == userId`) |
| CORS | `AllowAll` policy for development — restrict origins before production deploy |
| Soft delete | Deleted records never returned; no data loss |
