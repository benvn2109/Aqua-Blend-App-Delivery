# AquaBlend Authentication and Authorisation

AquaBlend uses JWT Bearer authentication with role-based authorisation.

## Sprint 1

### Scope

Sprint 1 introduced the JWT Bearer authentication proof of concept. It included:

* JWT signature, issuer, audience and expiry validation
* Admin, Analyst and Viewer roles
* CanView, CanAnalyse and CanAdminister policies
* Protected `GET /api/auth/me` endpoint
* Secure development keys using .NET User Secrets
* Initial verification of 401, 403 and 200 responses

User registration, password storage, refresh tokens and production account management were outside the Sprint 1 scope.

### Roles and policies

| Policy        | Permitted roles        |
| ------------- | ---------------------- |
| CanView       | Admin, Analyst, Viewer |
| CanAnalyse    | Admin, Analyst         |
| CanAdminister | Admin                  |

### Sprint 1 protected endpoint

| Endpoint           | Policy  |
| ------------------ | ------- |
| GET `/api/auth/me` | CanView |

A successful request returns the authenticated user’s identifier, username and roles.

## Sprint 2

### Scope

Sprint 2 applied the existing policies to the agreed backend endpoints. It also documented client JWT usage and verified unauthorised and forbidden responses using automated integration tests.

### Sprint 2 protected endpoints

| Endpoint                                              | Policy        |
| ----------------------------------------------------- | ------------- |
| GET `/api/water-sources`                              | CanView       |
| GET `/api/water-sources/{id}`                         | CanView       |
| POST `/api/water-sources`                             | CanAdminister |
| PUT `/api/water-sources/{id}`                         | CanAdminister |
| DELETE `/api/water-sources/{id}`                      | CanAdminister |
| GET `/api/changes`                                    | CanView       |
| GET `/api/scenarios`                                  | CanView       |
| GET `/api/scenarios/{id}`                             | CanView       |
| POST `/api/scenarios`                                 | CanAnalyse    |
| PUT `/api/scenarios/{id}`                             | CanAnalyse    |
| DELETE `/api/scenarios/{id}`                          | CanAdminister |
| GET `/api/optimisation-results`                       | CanView       |
| GET `/api/optimisation-results/{id}`                  | CanView       |
| GET `/api/optimisation-results/scenario/{scenarioId}` | CanView       |

### Sending a JWT from the frontend

The client must send the JWT in the HTTP Authorization header:

```http
Authorization: Bearer <token>
```

JavaScript example:

```javascript
const response = await fetch("/api/scenarios", {
  headers: {
    Authorization: `Bearer ${token}`
  }
});
```

The word `Bearer`, followed by one space and the token, is required. Tokens must not be placed in URLs, logged or committed to Git.

### Manual Postman verification

| Request                                   | Expected result  | Result |
| ----------------------------------------- | ---------------- | ------ |
| GET `/api/scenarios` without a token      | 401 Unauthorized | Passed |
| POST `/api/scenarios` with a Viewer token | 403 Forbidden    | Passed |
| GET `/api/auth/me` with an Admin token    | 200 OK           | Passed |

## Sprint 3

### Authorisation rollout

Sprint 3 expanded automated authorisation coverage across the existing policy tiers. The tests confirm 401 responses for anonymous requests, 403 responses for authenticated users without the required role, and successful responses for users with permitted roles.

JWT Bearer authentication was added to the OpenAPI document. Endpoints containing an `Authorize` attribute are marked as requiring a Bearer token. The OpenAPI document is available only in the Development environment.

Authentication namespaces were aligned with the rest of the solution by using `AquaBlend.Authorization`, `AquaBlend.Controllers` and `AquaBlend.DTOs.Auth`.

### Non-development authentication behaviour

Development tokens created with `dotnet user-jwts` are for local testing only and must not be used in staging or production.

In a non-development environment, clients must obtain JWTs from the approved identity provider and send them using:

```http
Authorization: Bearer <token>
```

The API must validate the token signature, issuer, audience and expiry before allowing access. Missing, invalid or expired tokens return `401 Unauthorized`. Authenticated users without the role required by an endpoint receive `403 Forbidden`.

Signing keys and identity-provider configuration must be supplied through environment variables or an approved secret-management service. Secrets, tokens and production credentials must never be stored in appsettings files, source code, logs or Git.

The AquaBlend API currently validates access tokens but does not provide user registration, password management, token issuance or refresh-token services.
