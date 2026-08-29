# Definition of Done

## Sprint 1 Definition

Sprint 1 is complete when:

| # | Criterion | Status | Evidence |
|---|---|---|---|
| 1 | The ASP.NET Core API, test project, documentation folders, and CI workflow are correctly scaffolded. | ✅ Met | `AquaBlend.Api.csproj`, `AquaBlend.Tests`, `Backend/docs/`, `.github/workflows/ci.yml` all exist. |
| 2 | The application restores, builds, and runs successfully. | ✅ Met | Confirmed locally (`dotnet build`) and via CI restore/build steps. |
| 3 | `GET /api/health` remains operational and returns a successful response. | ✅ Met | Minimal API endpoint still present in [Program.cs](../Program.cs). |
| 4 | Controller support is enabled, and implemented controllers appear in OpenAPI. | ✅ Met | `AddControllers()`/`MapControllers()` in Program.cs; `AuthController`, `ChangesController`, `ScenariosController`, `WaterSourcesController` are attribute-routed and picked up by `AddOpenApi()`/`MapOpenApi()`. |
| 5 | Entity Framework Core connects successfully to PostgreSQL. | ✅ Met | `AddDbContext` configures `UseNpgsql` outside the Testing environment; connection string sourced from configuration/User Secrets. |
| 6 | The initial migration and development seed data work correctly. | ✅ Met | `Migrations/20260728052815_InitialCreate.cs` plus a second migration for OptimisationResult; `SeedData.Initialize` seeds Water Sources, a Scenario, and an OptimisationResult, and runs on startup via `db.Database.Migrate()`. |
| 7 | Water Source and Scenario GET endpoints return the expected data and HTTP status codes. | ✅ Met | `WaterSourcesController` and `ScenariosController` implement `GetAll`/`GetById` with 200/404 handling, covered by tests in `ScenarioEndpointsTests.cs`. |
| 8 | The proposed authentication, authorisation, and automatic-update approaches are implemented as proof of concepts or clearly documented. | ✅ Met | JWT auth + role policies implemented (`AuthController`, `Authorization/AppRoles.cs`, `Authorization/AppPolicies.cs`) and documented in `docs/authentication.md`; REST polling proof of concept implemented in `ChangesController` and documented in `docs/automatic-updates.md`. |
| 9 | Automated tests pass using `dotnet test`. | ✅ Met | Verified locally with PostgreSQL not running (tests use the `Testing` environment's in-memory provider). |
| 10 | GitHub Actions successfully restores, builds, and tests the backend solution. | ✅ Met | `.github/workflows/ci.yml` runs restore/build/test on push and PR (see CI review notes below for gaps in branch coverage). |
| 11 | No passwords, connection strings, API keys, or generated build files are committed. | ✅ Met | `appsettings.json` only contains a placeholder (`CHANGE_ME`) connection string; real secrets are documented as living in User Secrets; `.gitignore` excludes `bin/`, `obj/`, `.env*`, local appsettings overrides, and certificates. |
| 12 | Completed work is reviewed and merged into `backend/sprint-1` through pull requests. | ✅ Met | Recent history shows PR merges (e.g. `#8`, `#5`, `#9`) into `backend/sprint-1`. |
| 13 | Relevant Sprint 1 documentation is complete and up to date. | ✅ Met | `docs/authentication.md`, `docs/automatic-updates.md`, `docs/database.md` are present and reflect the current implementation. |

All Sprint 1 criteria are met as of this update.

## Sprint 2 Definition

Sprint 2 is complete when:

| # | Criterion | Status | Notes |
|---|---|---|---|
| 1 | The backend restores, builds and runs successfully. | ✅ Met | Same as Sprint 1 criterion 2; still true on current `master`/`backend/sprint-1`. |
| 2 | GitHub CI passes. | ⚠️ Needs verification | CI workflow only triggers on `backend/sprint-1` and `feature/**`; Sprint 2 work on `sprint2/*`-style branches currently gets no CI run on push (see CI review). Fix required before this can be considered reliably met. |
| 3 | PostgreSQL migrations apply successfully. | ✅ Met | Two migrations exist (`InitialCreate`, `AddOptimisationResultAndScenarioExternalId`); `Program.cs` calls `db.Database.Migrate()` on startup against a relational provider. |
| 4 | Water Source and Scenario GET endpoints work. | ✅ Met | `WaterSourcesController.GetAll/GetById` and `ScenariosController.GetAll/GetById`, both covered by tests. |
| 5 | A Scenario can be created through POST. | ✅ Met | `ScenariosController.Create`, covered by `ScenarioEndpointsTests.Create_ReturnsCreatedScenario` and the invalid-data test. |
| 6 | An Optimisation Result can be submitted through POST. | ❌ Not met | No `OptimisationResultsController` (or any POST endpoint) exists. `OptimisationResult` currently only exists as an entity, migration, and seed-data record; `docs/database.md` documents the intended `POST /api/optimisation-results` contract, but it is not implemented. |
| 7 | Results can be retrieved by result ID and Scenario ID. | ❌ Not met | No controller/endpoints exist to retrieve `OptimisationResult` records at all. |
| 8 | Multiple results can be stored for one Scenario. | ⚠️ Partially met | The data model supports it (`OptimisationResult.ScenarioId` is a plain FK with `ON DELETE RESTRICT`, no unique constraint forcing one-per-scenario), but this is unverified end-to-end since there is no API to submit or read results yet. |
| 9 | The changes endpoint reports new or updated records. | ✅ Met | `ChangesController` returns Water Sources and Scenarios created/updated after `since`, tested by `ChangesControllerTests`. |
| 10 | JWT authentication and role policies work. | ⚠️ Partially met | JWT bearer auth and the `CanView`/`CanAnalyse`/`CanAdminister` policies are implemented and enforced on `GET /api/auth/me` (documented, manually tested per `docs/authentication.md`). However, `WaterSourcesController` and `ScenariosController` have no `[Authorize]` attributes yet, so role policies are not actually applied to the data endpoints. |
| 11 | CORS allows the agreed Next.js frontend origin. | ❌ Not met | No `AddCors`/`UseCors` call anywhere in the project; `Program.cs` has no CORS configuration at all. |
| 12 | Invalid requests return consistent errors. | ⚠️ Partially met | `[ApiController]` model validation gives a consistent `ValidationProblemDetails` shape for bad input, and `ChangesController` returns a custom `{ error }` shape for its own validation failures — these two shapes are inconsistent with each other. There is no global exception-handling middleware (`Backend/Middleware/` only contains a `.gitkeep`), so unhandled exceptions won't produce a consistent error body. |
| 13 | OpenAPI displays all frontend-required endpoints. | ❌ Not met | OpenAPI is only generated for controllers/endpoints that exist, so it correctly shows Water Sources, Scenarios, Changes, and Auth — but Optimisation Result endpoints don't exist yet, so OpenAPI cannot display them. Also note `MapOpenApi()` is only called `if (app.Environment.IsDevelopment())`, so the document isn't available in other environments. |
| 14 | Sample requests and responses are documented. | ⚠️ Partially met | `docs/authentication.md` and `docs/automatic-updates.md` include sample requests/responses; `docs/api-contract.md` is high-level only with no samples; `AquaBlend.Api.http` is still the default scaffold template pointing at a nonexistent `/weatherforecast` endpoint. Water Source, Scenario, and Optimisation Result endpoints have no documented sample requests/responses. |
| 15 | Automated tests pass. | ✅ Met | Verified locally with `dotnet test Backend/AquaBlend.sln` while PostgreSQL was not running; all tests pass via the in-memory provider. |
| 16 | No credentials or secrets are committed. | ✅ Met | Same verification as Sprint 1 criterion 11 — `appsettings*.json` contain placeholders only, `.gitignore` covers local secrets/certs. |
| 17 | The frontend can successfully call at least one backend endpoint. | ❌ Not met | Blocked by the missing CORS configuration (criterion 11) — a browser-based Next.js frontend on a different origin cannot currently call the API. No frontend integration evidence found in this repository. |
| 18 | All completed work is reviewed through pull requests. | ✅ Met (process, ongoing) | Sprint 1 work followed this process (see PR merges above); no evidence of direct-to-branch commits bypassing review so far. This should keep being verified per PR going forward rather than treated as a one-time check. |

### Summary of open Sprint 2 gaps

- Optimisation Result submission and retrieval endpoints (criteria 6, 7, 8) are not implemented.
- CORS is not configured for the Next.js frontend origin (criterion 11), which also blocks criterion 17.
- Role policies (`[Authorize]`) are not yet applied to the Water Source/Scenario controllers (criterion 10).
- Error response shapes are inconsistent and there's no global exception handler (criterion 12).
- OpenAPI and sample-documentation coverage is incomplete, largely because the Optimisation Result endpoints don't exist yet (criteria 13, 14).
- CI branch triggers don't cover Sprint 2 branch naming (criterion 2) — see the CI workflow review for the fix.
