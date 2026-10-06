# AquaBlend API Contract

## 1. Overview

This document defines the current backend API contract for scenarios,
optimisation results, authentication, changes, and health checks.
This document defines the REST API contract for the AquaBlend backend.

Base URL for local development:

`http://localhost:5194`

---

## Authentication and authorisation

The API uses JWT bearer authentication with role-based policies.

| Policy | Purpose |
|---|---|
| `CanView` | Read API resources |
| `CanAnalyse` | Create/update analysis-related resources |
| `CanAdminister` | Administrative operations |

Unauthenticated requests to protected endpoints return `401 Unauthorized`.
Authenticated users without the required policy return `403 Forbidden`.

---

# Health

## GET `/api/health`

Returns the API health status.

### Success

`200 OK`

Example:

```json
{
  "status": "ok"
}
The API uses JSON for request and response bodies.

## 2. Authentication and Authorization

Protected endpoints require authentication using the configured bearer
authentication mechanism.

The API uses the following roles:

- `Admin`
- `Analyst`
- `Viewer`

Authorization is applied according to the operation being performed.

### Access conventions

| Operation | Required access |
|---|---|
| Read operations | Authenticated viewer-level access |
| Create/update analysis data | Analyst or Admin |
| Administrative operations | Admin |

Unauthorized requests return `401 Unauthorized`.

Authenticated users without sufficient permissions return
`403 Forbidden`.

---

# 3. Health

## GET `/api/health`

Returns the current API health status.

### Success

**200 OK**

```json
{
  "status": "healthy",
  "service": "AquaBlend.Api",
  "timestamp": "2026-09-20T10:00:00Z"
}
The API is implemented using ASP.NET Core and exposes JSON REST endpoints under the `/api` route prefix.

The backend provides:

- Water source management
- Scenario management
- Optimisation result retrieval
- Automatic change detection
- Health monitoring
- Authenticated user information

The API uses controller-based routing for controller endpoints.

The `/api/health` endpoint is preserved as the application health endpoint.

The contract is designed to remain extension-friendly as additional endpoints are added.

All timestamps returned by the API are UTC.

## 2. Base URL

Local development:

`http://localhost:5194`

API prefix:

`/api`

Requests and responses use:

`application/json`

JSON properties use camelCase.

## 3. Authentication

Protected endpoints require a JWT bearer token.

Request header:

```http
Authorization: Bearer <token>
