# API Specification

## Overview

RESTful API for the Task App, built with Azure Functions v4 Isolated Worker (.NET 10).

## Base URL

- **Development**: `http://localhost:7071/api`

## Authentication

All task endpoints require a JWT Bearer token obtained from the auth endpoints.

```
Authorization: Bearer <token>
Content-Type: application/json
```

Tokens are valid for **7 days** from issuance.

## Error Format

All error responses follow this shape:

```json
{ "error": "Human-readable message" }
```

## Status Codes

| Code | Meaning |
|------|---------|
| 200 | OK |
| 201 | Created |
| 204 | No Content |
| 400 | Bad Request — invalid input or missing required field |
| 401 | Unauthorized — missing or invalid token |
| 404 | Not Found — resource doesn't exist or caller has no access |
| 409 | Conflict — email already registered |
| 500 | Internal Server Error |

---

## Authentication Endpoints

### POST `/auth/register`

Register a new user with email and password.

**Request**

```json
{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "fullName": "Jane Smith"
}
```

| Field | Type | Required |
|-------|------|----------|
| email | string | Yes |
| password | string | Yes |
| fullName | string | Yes |

**Response** `201 Created`

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "user@example.com",
  "fullName": "Jane Smith",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Errors**

| Status | Condition |
|--------|-----------|
| 400 | Email or password missing |
| 409 | Email already registered |

---

### POST `/auth/login`

Authenticate with email and password.

**Request**

```json
{
  "email": "user@example.com",
  "password": "SecurePass123!"
}
```

**Response** `200 OK`

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "user@example.com",
  "fullName": "Jane Smith",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Errors**

| Status | Condition |
|--------|-----------|
| 400 | Email or password missing |
| 401 | Invalid credentials |

---

### POST `/auth/google`

Sign in or register with a Google ID token. Supports three cases:
- **New user** — creates account with Google identity
- **Returning Google user** — logs in
- **Existing local account** — links Google ID and logs in

**Request**

```json
{
  "idToken": "<Google ID token from frontend OAuth popup>"
}
```

**Response** `200 OK`

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "user@gmail.com",
  "fullName": "Jane Smith",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Errors**

| Status | Condition |
|--------|-----------|
| 400 | idToken missing |
| 401 | Google token invalid or expired |

---

## Task Endpoints

All task endpoints require `Authorization: Bearer <token>`.

Access control: a user can only see and modify tasks they **created** or that are **assigned to them**.

---

### GET `/tasks`

Get tasks for the authenticated user. If any filter parameter is provided, runs `SearchAsync`; otherwise returns all tasks sorted by `CreatedAt` descending.

**Query Parameters**

| Parameter | Type | Description |
|-----------|------|-------------|
| search | string | Text search across title and description |
| status | int | `0` = Pending, `1` = InProgress, `2` = Done |
| assignedTo | guid | Filter by assigned user ID |
| dueDateFrom | datetime | Tasks with DueDate >= this value |
| dueDateTo | datetime | Tasks with DueDate <= this value |
| sortBy | string | `title` \| `duedate` \| `status` \| `createdat` (default) |
| sortDescending | bool | `true` for descending order |

**Example**

```
GET /api/tasks?search=bug&status=1&sortBy=duedate&sortDescending=false
```

**Response** `200 OK`

```json
[
  {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "title": "Fix login bug",
    "description": "Users can't log in on Safari",
    "dueDate": "2025-06-15T00:00:00Z",
    "status": 1,
    "createdBy": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "creatorName": "Jane Smith",
    "assignedTo": "7cb96e41-3821-4d12-b9a3-1d874f22cc10",
    "assignedToName": "John Doe",
    "createdAt": "2025-05-01T10:00:00Z",
    "updatedAt": null
  }
]
```

> Status is returned as an integer: `0` Pending, `1` InProgress, `2` Done.

---

### GET `/tasks/{id}`

Get a single task by ID.

**Path Parameters**

| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Task ID |

**Response** `200 OK` — same shape as a single item from `GET /tasks`

**Errors**

| Status | Condition |
|--------|-----------|
| 400 | `id` is not a valid GUID |
| 404 | Task not found or caller has no access |

---

### POST `/tasks`

Create a new task. The `createdBy` field is set automatically from the JWT — it cannot be overridden by the client.

**Request**

```json
{
  "title": "Fix login bug",
  "description": "Users can't log in on Safari",
  "dueDate": "2025-06-15T00:00:00Z",
  "assignedTo": "7cb96e41-3821-4d12-b9a3-1d874f22cc10"
}
```

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| title | string | Yes | Must not be blank |
| description | string | No | |
| dueDate | datetime | Yes | ISO 8601 |
| assignedTo | guid | No | Defaults to the creator if omitted |

**Response** `201 Created` — full `TaskDto` with navigation properties populated

**Errors**

| Status | Condition |
|--------|-----------|
| 400 | Body is malformed or title is missing |
| 401 | Token missing or invalid |

---

### PUT `/tasks/{id}`

Update an existing task. Only the task **creator** can update it.

**Path Parameters**

| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Task ID |

**Request**

```json
{
  "title": "Fix login bug (updated)",
  "description": "Reproduced on Chrome too",
  "dueDate": "2025-06-20T00:00:00Z",
  "status": 1,
  "assignedTo": "7cb96e41-3821-4d12-b9a3-1d874f22cc10"
}
```

| Field | Type | Required |
|-------|------|----------|
| title | string | Yes |
| description | string | No |
| dueDate | datetime | Yes |
| status | int | Yes — `0`, `1`, or `2` |
| assignedTo | guid | No |

**Response** `200 OK` — updated `TaskDto`

**Errors**

| Status | Condition |
|--------|-----------|
| 400 | Body malformed or ID not a valid GUID |
| 401 | Token missing or invalid |
| 404 | Task not found or caller is not the creator |

---

### DELETE `/tasks/{id}`

Soft-delete a task. Only the task **creator** can delete it. The record is flagged `IsDeleted = true` and is no longer returned by any query — it is never physically removed.

**Path Parameters**

| Parameter | Type | Description |
|-----------|------|-------------|
| id | guid | Task ID |

**Response** `204 No Content`

**Errors**

| Status | Condition |
|--------|-----------|
| 400 | ID not a valid GUID |
| 401 | Token missing or invalid |
| 404 | Task not found or caller is not the creator |

---

## Interactive Documentation

Swagger UI is available at `http://localhost:7071/api/swagger/ui` when the backend is running. All endpoints can be tried directly from the browser after authorizing with a Bearer token.
