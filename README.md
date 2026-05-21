# Task Management System

> Full-Stack Task Management Application — Technical Assessment for Senior Software Engineer Position

A production-ready task management web application built with **React 19**, **Azure Functions (C# .NET 10)**, **Entity Framework Core**, and **Swagger/OpenAPI** documentation. Features JWT + Google OAuth authentication, advanced filtering, real-time polling, Clean Architecture, and Factory Pattern.

---

## Table of Contents

- [Features](#features)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [API Documentation](#api-documentation)
- [Testing](#testing)
- [Project Structure](#project-structure)
- [Architecture Decisions](#architecture-decisions)

---

## Features

### Authentication & Security

- JWT-based authentication with 7-day token expiry
- Secure password hashing with BCrypt
- Google OAuth 2.0 login (find-or-create + local account linking)
- Token validation middleware for all protected endpoints
- CORS configuration for cross-origin requests

### Task Management

- Full CRUD operations (Create, Read, Update, Delete with soft delete)
- Task assignment to other users
- Task status management (Pending, In Progress, Done)
- Due date tracking
- Task ownership (CreatedBy and AssignedTo)

### Search & Filtering

- Server-side filtering with 6 parameters: text search, status, assignedTo, due date range, sort field, sort direction
- Real-time client-side filtering with instant results (no round-trip)
- Combined filters for advanced queries

### API Documentation

- Interactive Swagger UI at `/api/swagger/ui`
- Full OpenAPI 3.0 specification with Bearer token auth support

### Frontend

- React 19 with TypeScript
- TaskContext with 30-second polling for live updates
- Responsive design with TailwindCSS
- Loading states, error handling, and form validation

### CI/CD

- GitHub Actions for API (build + test on every push/PR)
- GitHub Actions for client (type check + build on every push/PR)

---

## Tech Stack

### Frontend

| Technology | Version | Purpose |
|------------|---------|---------|
| React | 19.1.1 | UI Framework |
| TypeScript | 5.x | Type Safety |
| Vite | 7.x | Build Tool |
| TailwindCSS | 3.x | Styling |
| Axios | Latest | HTTP Client |
| React Router | 7.x | Client-Side Routing |
| @react-oauth/google | 0.12 | Google OAuth |

### Backend

| Technology | Version | Purpose |
|------------|---------|---------|
| Azure Functions | v4 Isolated Worker | Serverless API |
| .NET | 10.0 | Runtime |
| C# | 13 | Language |
| Entity Framework Core | 10.0 | ORM |
| BCrypt.Net | Latest | Password Hashing |
| Google.Apis.Auth | Latest | Google token validation |
| Swagger/OpenAPI | Latest | API Documentation |

### Testing

| Technology | Version | Purpose |
|------------|---------|---------|
| xUnit | 2.x | Test Framework |
| Moq | 4.x | Mocking |
| EF Core InMemory | 10.0 | Test Database |

---

## Architecture

Clean Architecture with strict dependency rules — outer layers depend on inner layers, never the reverse.

```
┌────────────────────────────────┐
│  Presentation                  │
│  TaskApp.Api                   │
│  Azure Functions, Middleware   │
└────────────┬───────────────────┘
             │
┌────────────▼───────────────────┐
│  Application                   │
│  TaskApp.Application           │
│  Interfaces, DTOs, Factories   │
└────────────┬───────────────────┘
             │
┌────────────▼───────────────────┐
│  Domain                        │
│  TaskApp.Domain                │
│  Entities, Enums               │
└────────────┬───────────────────┘
             │
┌────────────▼───────────────────┐
│  Infrastructure                │
│  TaskApp.Infrastructure        │
│  Repository, EF Core, Services │
└────────────────────────────────┘
```

### Design Patterns

- **Repository Pattern** — `IRepository<T>` generic repository with rich query API
- **Unit of Work Pattern** — `IUnitOfWork` for transactional consistency
- **Factory Pattern** — `ITaskFactory` / `TaskFactory` centralizes entity construction
- **Result Pattern** — `Result<T>` for explicit success/failure without exceptions
- **Soft Delete** — `IsDeleted` flag on `BaseEntity`; records are never physically removed

---

## Prerequisites

- .NET 10 SDK
- Node.js 20+ and pnpm
- Azure Functions Core Tools v4

---

## Getting Started

### 1. Start the Backend

```bash
cd api/src/TaskApp.Api
func start
```

Backend: `http://localhost:7071/api`  
Swagger UI: `http://localhost:7071/api/swagger/ui`

### 2. Start the Frontend

```bash
cd client
pnpm install
pnpm dev
```

Frontend: `http://localhost:3000`

---

## API Documentation

### Endpoints

#### Authentication

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/auth/register` | Register new user | No |
| POST | `/api/auth/login` | Email/password login | No |
| POST | `/api/auth/google` | Google OAuth login | No |

#### Tasks

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/tasks` | Get tasks (with filters) | Yes |
| GET | `/api/tasks/{id}` | Get task by ID | Yes |
| POST | `/api/tasks` | Create task | Yes |
| PUT | `/api/tasks/{id}` | Update task | Yes |
| DELETE | `/api/tasks/{id}` | Soft-delete task | Yes |

#### Query Parameters — `GET /api/tasks`

```
?search=urgent          # Text search (title + description)
&status=1               # 0=Pending, 1=InProgress, 2=Done
&assignedTo=<guid>      # Filter by assigned user
&dueDateFrom=2025-01-01 # From date
&dueDateTo=2025-12-31   # To date
&sortBy=duedate         # title | duedate | status | createdat
&sortDescending=true
```

---

## Testing

### Run Tests

```bash
# From repo root
dotnet test api/TaskApp.sln
```

### Test Coverage — 29 tests, 100% pass rate

| File | Tests | Covers |
|------|-------|--------|
| `TaskServiceTests.cs` | 12 | CRUD, search, filtering, sorting, soft delete |
| `UserRepositoryTests.cs` | 11 | Register, login, Google OAuth (create/link/dedupe) |
| `JwtServiceTests.cs` | 6 | Token generation, validation, tamper detection |

---

## Project Structure

```
task-app/
├── .github/
│   └── workflows/
│       ├── api.yml          # Build + test .NET on push/PR
│       └── client.yml       # Type check + build React on push/PR
│
├── api/
│   ├── TaskApp.sln
│   └── src/
│       ├── TaskApp.Domain/
│       │   ├── Common/BaseEntity.cs
│       │   ├── Entities/Task.cs
│       │   ├── Entities/User.cs
│       │   └── Enums/TaskStatus.cs
│       │
│       ├── TaskApp.Application/
│       │   ├── Common/Result.cs
│       │   ├── Dtos/           (TaskDto, AuthDto, etc.)
│       │   ├── Factories/TaskFactory.cs
│       │   └── Interfaces/     (IRepository, ITaskService, IAuthService, ITaskFactory, ...)
│       │
│       ├── TaskApp.Infrastructure/
│       │   ├── Data/ApplicationDbContext.cs
│       │   ├── Repositories/Repository.cs
│       │   └── Services/       (TaskService, AuthService, JwtService, GoogleAuthService)
│       │
│       └── TaskApp.Api/
│           ├── Functions/      (TaskFunctions.cs, AuthFunctions.cs)
│           ├── Middleware/     (AuthMiddleware.cs, ExceptionHandlingMiddleware.cs)
│           ├── Extensions/     (ServiceCollectionExtensions.cs)
│           ├── OpenApi/        (JwtAuthorizationAttribute.cs, BearerAuthFlow.cs)
│           └── Program.cs
│
├── tests/
│   └── TaskApp.Tests/
│       ├── TaskServiceTests.cs
│       ├── UserRepositoryTests.cs
│       └── JwtServiceTests.cs
│
├── client/
│   └── src/
│       ├── components/
│       │   ├── common/   (Button, Input, Modal, Spinner, ProtectedRoute)
│       │   ├── layout/   (Layout, Navbar)
│       │   └── tasks/    (TaskCard, TaskFilters, TaskForm)
│       ├── contexts/
│       │   ├── AuthContext.tsx
│       │   └── TaskContext.tsx   (30s polling)
│       ├── pages/        (DashboardPage, LoginPage, RegisterPage, NotFoundPage)
│       ├── services/     (api.ts, authService.ts, taskService.ts)
│       └── types/        (index.ts)
│
├── sql/                  (Schema, stored procedures, seed data)
└── docs/
```

---

## Architecture Decisions

### Why InMemory Database for the demo?

Zero setup, instant startup, no SQL Server required. The production migration path is ready — `TaskConfiguration.cs` defines the EF Core table mapping, and SQL scripts are in `/sql`.

### Why JWT over sessions?

Stateless — scales horizontally without shared session storage. Tokens are validated in `AuthMiddleware` on every request.

### Why Google OAuth via `Google.Apis.Auth`?

The backend validates the Google ID token directly with `GoogleJsonWebSignature.ValidateAsync`, then issues its own JWT. This keeps the auth flow simple: the frontend handles the Google popup, sends the ID token once to `/auth/google`, and from that point on uses the app's own JWT — no dependency on Google's session infrastructure.

### Why polling instead of WebSockets for real-time?

Azure Functions Isolated Worker doesn't natively support long-lived WebSocket connections. Polling every 30 seconds is a pragmatic fit for this runtime without introducing SignalR or a separate hub infrastructure.
