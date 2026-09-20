# AquaBlend API Contract

## Overview

This document defines the current backend API contract for scenarios,
optimisation results, authentication, changes, and health checks.

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