# AquaBlend Authentication and Authorisation

## Sprint 1 scope

AquaBlend uses JWT Bearer authentication for the Sprint 1 proof
of concept. This implementation validates development JWTs and
applies role-based authorisation policies.

User registration, login, password storage, refresh tokens and
production account management are outside the Sprint 1 scope.

## JWT validation

The API validates:

- Token signature
- Issuer
- Audience
- Token expiry
- Role claims

The development signing key is stored using .NET User Secrets and
is not committed to Git.

## Roles

The initial application roles are:

- Admin
- Analyst
- Viewer

## Policies

| Policy | Permitted roles |
|---|---|
| CanView | Admin, Analyst, Viewer |
| CanAnalyse | Admin, Analyst |
| CanAdminister | Admin |

## Protected endpoint

`GET /api/auth/me` is protected by the `CanView` policy.

A successful request returns the authenticated user's identifier,
username and roles.

## Generate a development token

From the folder containing `AquaBlend.Api.csproj`, generate an Admin
token:

```bash
dotnet user-jwts create \
  --name admin-user \
  --role Admin \
  --audience AquaBlend.Api \
  --issuer dotnet-user-jwts \
  --valid-for 1h
```

Generate a token without an authorised role for the forbidden test:

```bash
dotnet user-jwts create \
  --name guest-user \
  --role Guest \
  --audience AquaBlend.Api \
  --issuer dotnet-user-jwts \
  --valid-for 1h
```

Generated tokens and signing keys are local development credentials.
They must not be committed, included in screenshots, or shared publicly.

## Test results

| Test | Expected result | Result |
|---|---|---|
| No token | 401 Unauthorized | Passed |
| Valid Guest token | 403 Forbidden | Passed |
| Valid Admin token | 200 OK | Passed |

## Secret management

The development signing key is managed through .NET User Secrets.
The local PostgreSQL connection string is also stored through User Secrets.

Production deployments must use environment variables or an approved
secret-management service.