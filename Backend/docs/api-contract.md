# AquaBlend API Contract

## 1. Overview

This document defines the REST API contract for the AquaBlend backend.

Base URL for local development:

`http://localhost:5194`

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