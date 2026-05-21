# Task App

> A full-stack task management system built with production-grade architecture — Clean Architecture on the backend, React 19 on the frontend, serverless on Azure.

## What is this?

A real-world task management app — not a tutorial, not a toy. Built to demonstrate how a production system is actually structured: layered architecture, explicit failure handling, auth that works end-to-end, and a CI pipeline that runs on every push.

**The interesting parts:**

- **Clean Architecture** enforced at the project level — Domain has zero external dependencies, Application only knows about Domain, Infrastructure plugs in at the edges
- **JWT + Google OAuth** — the backend validates Google ID tokens directly via `Google.Apis.Auth`, then issues its own JWT. No session storage, no external session dependency after the first handshake
- **Result Pattern** (`Result<T>`) everywhere instead of exception-driven flow
- **Factory Pattern** for entity construction — `ITaskFactory` centralizes business rules for task creation
- **Soft deletes** out of the box — records are flagged, never destroyed
- **Real-time updates via polling** — pragmatic choice given Azure Functions Isolated Worker doesn't natively support WebSockets

---

## Stack

### Backend

| Layer | Technology |
|-------|------------|
| Runtime | Azure Functions v4 Isolated Worker (.NET 8) |
| Language | C# 13 |
| ORM | Entity Framework Core |
| Auth | JWT + BCrypt + Google.Apis.Auth |
| Docs | Swagger / OpenAPI 3.0 |
| Tests | xUnit + Moq + EF Core InMemory |

### Frontend

| Layer | Technology |
|-------|------------|
| Framework | React 19 + TypeScript |
| Build | Vite 7 |
| Styling | TailwindCSS 3 |
| Routing | React Router 7 |
| HTTP | Axios |
| OAuth | @react-oauth/google |

---

## Architecture

Clean Architecture with strict dependency rules — outer layers depend on inner, never the reverse.

```
TaskApp.Api              ← Azure Functions, Middleware, OpenAPI
       ↓
TaskApp.Application      ← Interfaces, DTOs, Factories, Result<T>
       ↓
TaskApp.Domain           ← Entities, Enums (zero dependencies)
       ↓
TaskApp.Infrastructure   ← Repository, EF Core, Auth Services
```

### Design Patterns in use

| Pattern | Where |
|---------|-------|
| Repository + Unit of Work | `IRepository<T>`, `IUnitOfWork` |
| Factory | `ITaskFactory` / `TaskFactory` |
| Result | `Result<T>` — explicit success/failure without exceptions |
| Soft Delete | `IsDeleted` on `BaseEntity` |

---

## Getting Started

**Prerequisites:** .NET 8 SDK, Node.js 20+, pnpm, Azure Functions Core Tools v4

### Backend

```bash
cd api/src/TaskApp.Api
func start
```

- API: `http://localhost:7071/api`
- Swagger UI: `http://localhost:7071/api/swagger/ui`

### Frontend

```bash
cd client
pnpm install
pnpm dev
```

- App: `http://localhost:3000`

---

## API

### Auth

| Method | Endpoint | Auth |
|--------|----------|------|
| POST | `/api/auth/register` | No |
| POST | `/api/auth/login` | No |
| POST | `/api/auth/google` | No |

### Tasks

| Method | Endpoint | Auth |
|--------|----------|------|
| GET | `/api/tasks` | Yes |
| GET | `/api/tasks/{id}` | Yes |
| POST | `/api/tasks` | Yes |
| PUT | `/api/tasks/{id}` | Yes |
| DELETE | `/api/tasks/{id}` | Yes |

### Filtering (`GET /api/tasks`)

```
?search=urgent            # Full-text search on title + description
&status=1                 # 0=Pending  1=InProgress  2=Done
&assignedTo=<guid>        # Filter by assigned user
&dueDateFrom=2025-01-01
&dueDateTo=2025-12-31
&sortBy=duedate           # title | duedate | status | createdat
&sortDescending=true
```

---

## Tests

```bash
dotnet test api/TaskApp.sln
```

29 tests, 100% pass rate.

| Suite | Tests | What it covers |
|-------|-------|----------------|
| `TaskServiceTests` | 12 | CRUD, search, filtering, sorting, soft delete |
| `UserRepositoryTests` | 11 | Register, login, Google OAuth (create / link / dedup) |
| `JwtServiceTests` | 6 | Token generation, validation, tamper detection |

---

## Project Structure

```
task-app/
├── .github/workflows/
│   ├── api.yml              # .NET build + test on every push/PR
│   └── client.yml           # TypeScript check + Vite build on every push/PR
│
├── api/
│   └── src/
│       ├── TaskApp.Domain/
│       ├── TaskApp.Application/
│       ├── TaskApp.Infrastructure/
│       └── TaskApp.Api/
│
├── tests/
│   └── TaskApp.Tests/
│
├── client/
│   └── src/
│       ├── components/      # common/, layout/, tasks/
│       ├── contexts/        # AuthContext, TaskContext (30s polling)
│       ├── pages/
│       ├── services/
│       └── types/
│
└── sql/                     # Schema, stored procedures, seed data
```

---

## Key Decisions

**Why InMemory DB for the demo?**
Zero setup. The production migration path is ready — EF Core table mapping is defined in `TaskConfiguration.cs` and SQL scripts live in `/sql`.

**Why JWT over sessions?**
Stateless — scales horizontally without shared session storage. `AuthMiddleware` validates the token on every protected request.

**Why polling instead of WebSockets?**
Azure Functions Isolated Worker doesn't natively support long-lived WebSocket connections. 30-second polling is the pragmatic fit without pulling in SignalR or a separate hub.

**Why `Google.Apis.Auth` on the backend?**
The frontend handles the Google popup and sends the ID token once to `/auth/google`. The backend validates it directly with `GoogleJsonWebSignature.ValidateAsync` and issues its own JWT. From that point on, the app has no dependency on Google's session infrastructure.
