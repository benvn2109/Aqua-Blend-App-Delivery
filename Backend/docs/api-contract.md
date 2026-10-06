# AquaBlend API Contract

## 1. Overview

This document defines the REST API contract for the AquaBlend backend.

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
