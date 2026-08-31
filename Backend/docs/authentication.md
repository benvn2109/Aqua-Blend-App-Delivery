# AquaBlend Authentication and Authorisation

AquaBlend uses JWT Bearer authentication with role-based authorisation.

## Sprint 1

### Scope

Sprint 1 introduced the JWT Bearer authentication proof of concept. It included:

- JWT signature, issuer, audience and expiry validation
- Admin, Analyst and Viewer roles
- CanView, CanAnalyse and CanAdminister policies
- Protected `GET /api/auth/me` endpoint
- Secure development keys using .NET User Secrets
- Initial verification of 401, 403 and 200 responses

User registration, password storage, refresh tokens and production account
management were outside the Sprint 1 scope.

### Roles and policies

| Policy | Permitted roles |
|---|---|
| CanView | Admin, Analyst, Viewer |
| CanAnalyse | Admin, Analyst |
| CanAdminister | Admin |

### Sprint 1 protected endpoint

| Endpoint | Policy |
|---|---|
| GET `/api/auth/me` | CanView |

A successful request returns the authenticated user's identifier, username and
roles.

## Sprint 2

### Scope

Sprint 2 applies the existing policies to the agreed backend endpoints. It also
documents client JWT usage and verifies unauthorised and forbidden responses
using automated integration tests.

### Sprint 2 protected endpoints

| Endpoint | Policy |
|---|---|
| GET `/api/water-sources` | CanView |
| GET `/api/water-sources/{id}` | CanView |
| POST `/api/water-sources` | CanAdminister |
| PUT `/api/water-sources/{id}` | CanAdminister |
| DELETE `/api/water-sources/{id}` | CanAdminister |
| GET `/api/changes` | CanView |
| GET `/api/scenarios` | CanView |
| GET `/api/scenarios/{id}` | CanView |
| POST `/api/scenarios` | CanAnalyse |

The Results controller is not currently available. Its POST endpoint must use
the CanAnalyse policy when the controller is added.

The required policies for Scenario PUT and DELETE endpoints are awaiting team
confirmation.

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

The word `Bearer`, followed by one space and the token, is required. Tokens
must not be placed in URLs, logged or committed to Git.
### Manual Postman verification

| Request | Expected result | Result |
|---|---|---|
| GET `/api/scenarios` without a token | 401 Unauthorized | Passed |
| POST `/api/scenarios` with a Viewer token | 403 Forbidden | Passed |
| GET `/api/auth/me` with an Admin token | 200 OK | Passed |
| PUT `/api/scenarios/{id}` | CanAnalyse |
| DELETE `/api/scenarios/{id}` | CanAdminister |