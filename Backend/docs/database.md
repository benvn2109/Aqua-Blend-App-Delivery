# Database Setup and Migrations

## Applying migrations locally

1. Ensure PostgreSQL 17 is running and the aquablend database exists.
2. Set your connection string via user-secrets (see main README).
3. From Backend/, run: `dotnet ef database update`
4. Verify: `psql -U postgres -d aquablend -c "\dt"`

## Creating a new migration

Run:

```text
dotnet ef migrations add MigrationName --project AquaBlend.Api.csproj
```

Review the generated file in `Migrations/` before applying — check column types and any foreign key
delete behaviour match intent (EF Core defaults foreign keys to Cascade, which is not always correct).

## Schema notes

* `OptimisationResult.ResultJson` stores the full MILP model output contract as PostgreSQL jsonb.
* `OptimisationResult.TotalCost` / `Currency` are nullable — non-OPTIMAL solves omit the objective
  block entirely in the source contract, so there is nothing to extract. Do not default to 0.
* OptimisationResult to Scenario foreign key uses `ON DELETE RESTRICT`, not cascade — a Scenario with
  existing results cannot be deleted without explicitly handling its results first.
* `Scenario.ExternalId` resolves the JSON contract's `scenario_id` string on ingest.
  `POST /api/optimisation-results` must reject with 400 if no Scenario matches — never auto-create.

### Scenario.ExternalId nullability (fixed)

ExternalId was originally NOT NULL with a default of empty string, which caused a unique-index
violation on any database with more than one Scenario row (surfaced as a 500 on the second
`POST /api/scenarios` call). It is now nullable, and the migration automatically converts any
existing blank ExternalId values to NULL. No manual backfill or database recreation is needed —
just apply the migration.

## OptimisationRun and the RunId re-pointing (Sprint 3)

OptimisationResult now belongs to an OptimisationRun rather than directly to a Scenario, since the
AI team pushes results against a run rather than the backend calling a solver directly. A run is
created via `POST /api/scenarios/{id}/runs`, and results are posted against that run's id.

Schema:

* OptimisationRun: ScenarioId, WorkflowStatus, SolverStatus, FailureReason, FailureSource,
  ScenarioSnapshotJson, and automatic CreatedAt/UpdatedAt timestamps.
* WorkflowStatus allowed values are:
  `queued`, `solving`, `solved`, `analysing`, `completed`, `failed`.
  There is no `draft` or `ready` run state: readiness belongs to the scenario (Scenario.IsReady,
  and re-validation at run creation - see "Scenario network configuration and validation"
  below). A run is created only when submitted, and starts at `queued`.
* FailureReason (free text) and FailureSource are populated when WorkflowStatus becomes `failed`.
  FailureSource records which actor declared the failure, and its only values are `ai_team` and
  `backend` - the lowercase form of the RunStatusActor allowed to fail a run. A client never
  declares failure, so `client` is not a valid value. Use exactly these spellings; do not
  introduce others. Both fields are currently inert: nothing writes them yet, and no response
  exposes them. Whoever builds the status-update / result-ingest route should populate them
  from the authenticated actor.
* SolverStatus is nullable until a solver outcome exists. Its allowed values mirror the MILP
  contract exactly:
  `OPTIMAL`, `INFEASIBLE`, `UNBOUNDED`, `TIME_LIMIT`, `ERROR`.
* ScenarioSnapshotJson is stored as jsonb and is intended to capture the network configuration
  at run-creation time.
* `OptimisationResult.RunId` is required and unique (one result per run).
* `OptimisationResult.ScenarioId` is nullable and kept temporarily for backward compatibility
  with the Sprint 2 endpoints during the transition. It should be removed once those endpoints
  are fully migrated to look up by RunId instead. GET /api/optimisation-results/scenario/{id}
  already filters through the result's run (Run.ScenarioId), not this column, so results posted
  against a run are found whether or not ScenarioId is set. The run is authoritative everywhere:
  all three Sprint 2 routes (GET /api/optimisation-results, /{id} and /scenario/{id}) report
  ScenarioId from the result's run (Run.ScenarioId), never from this column, so filtering and
  reporting cannot disagree and no route reports 0.

### WorkflowStatus lifecycle and transitions

A run is created at `queued` by `POST /api/scenarios/{id}/runs`, after the scenario passes
re-validation. The primary workflow path is:

```text
queued → solving → solved → analysing → completed
```

The workflow is not strictly one-directional. The following transitions are allowed, and every
status is reachable from `queued`:

| From | To | Actor / owner |
| --- | --- | --- |
| `queued` | `solving` | AI team picks up the run |
| `solving` | `queued` | AI team, retry after a stalled solve |
| `solving` | `solved` | AI team, result received |
| `solved` | `analysing` | Backend derives diagnostics |
| `analysing` | `completed` | Backend |
| `queued` | `failed` | AI team, or backend (e.g. timeout) |
| `solving` | `failed` | AI team, or backend (e.g. timeout) |
| `solved` | `failed` | Backend |
| `analysing` | `failed` | Backend |

No other state skips are permitted. For example, `queued → solved` and
`solved → completed` are invalid because they bypass work owned by another stage of the workflow.

`completed` and `failed` are terminal states and cannot transition to another workflow state.

Withdrawing a run before pickup is not implemented: no endpoint changes a run's status yet, and
there is no client-owned transition. When withdrawal is built it will need its own terminal state
(e.g. `cancelled`, client-owned, from `queued`) - not a return to a pre-queued state, which no
longer exists.

These rules live in RunStatusService.IsValidTransition. Nothing calls it yet, so they are not
enforced anywhere until a status-update route is built on top of it.

A transition to the same state is treated as a successful no-op. This makes status updates
idempotent and allows callers such as the AI integration to safely retry a request after a
network failure without turning the retry itself into an error.

Transition validation must consider both the current/target state and the actor requesting the
change. A caller must not be able to set a state owned by another part of the system simply
because the from/to transition would otherwise be valid.

Invalid workflow transitions should be rejected with `409 Conflict`. The request itself is
well-formed, but the requested state is inconsistent with the current workflow state. A rejected
transition must leave the stored WorkflowStatus unchanged.

### WorkflowStatus versus SolverStatus

WorkflowStatus and SolverStatus are separate vocabularies with different owners.

`WorkflowStatus` belongs to AquaBlend and represents the application's processing lifecycle.
The optimisation model does not control this vocabulary, so AquaBlend can extend it when the
workflow requires another state.

`failed` represents a failure of the workflow or processing pipeline, such as a crash, timeout,
or malformed payload.

`SolverStatus`, by contrast, mirrors the MILP contract exactly and must not be extended with
AquaBlend-specific values.

In particular:

```text
WorkflowStatus = failed
```

is not equivalent to:

```text
SolverStatus = INFEASIBLE
```

`INFEASIBLE` means the solver ran successfully and determined that no feasible blend exists.
That is a valid solver outcome rather than a system failure, so the workflow can continue through
its normal post-processing stages to `completed`.

The migration backfills existing OptimisationResult rows automatically: for each result missing a
RunId, it creates a synthetic OptimisationRun (`WorkflowStatus = completed`, SolverStatus copied
from the result's own Status, an empty ScenarioSnapshotJson placeholder) and points the result at it.

No manual database changes are needed, just apply the migration.

Open items, not yet resolved:

* ScenarioSnapshotJson is currently a placeholder (`{}`) in seed data. The real snapshot should be
  populated by whichever endpoint creates a run.
* SolverStatus casing: the MILP contract emits uppercase values (`OPTIMAL`, `INFEASIBLE`, etc.), while
  an earlier architecture document used lowercase. Stored here exactly as the contract sends it;
  any casing translation for consumers should happen at the API layer, not the database.

## Reference-data entities (Sprint 3)

Added five new reference-data entities that back the frontend's scenario builder:
Plant, DemandZone, SourcePlantLink, PlantZoneLink, QualityProfile. All are keyed by an internal
Id, with Plant and DemandZone also carrying an ExternalId (nullable, unique) to align with the
MILP contract's `plant_id`/`zone_id` where applicable.

WaterSource was expanded from 2 fields to include availability status, withdrawal bounds,
activation cost, cost per ML, provenance flags, and a model-ready flag, matching the source
fields described in the MILP model output contract. WaterSource.ExternalId is nullable for the
same reason as Scenario.ExternalId: existing rows have no natural external identifier to backfill,
and NULL avoids the unique-index collision that a blank-string default would cause.

Open item: QualityProfile is currently modelled as a standalone named limit (e.g. "Standard
Drinking Water") rather than linked directly to a Plant, per the MILP contract's own note that
quality limits are global rather than per-plant. This may need revisiting once the frontend's
exact `GET /api/quality-profiles` shape is confirmed with Ashwitha and Pavan.

## Scenario network configuration and validation (Sprint 3)

Scenario now carries NetworkConfigJson (jsonb, default `'{}'`) and ValidationIssuesJson (jsonb,
default `'[]'`), holding the full network configuration (selected sources, plants, zones, links,
overrides, quality profile) and any validation issues respectively. IsReady (boolean) indicates
whether the scenario currently passes validation.

This is distinct from OptimisationRun.ScenarioSnapshotJson: NetworkConfigJson is the current,
editable draft; the run's snapshot is an immutable copy taken at run-creation time.

Both JSON columns use a database-level default via HasDefaultValueSql, not just a C# property
initializer, to avoid the same class of bug as ScenarioSnapshotJson and ExternalId in earlier
sprints: a C#-only default doesn't help rows inserted outside the application code.

The composite index originally specified as `(ScenarioId, SolvedAt DESC)` has been implemented as
`(ScenarioId, CreatedAt DESC)` on OptimisationRun rather than OptimisationResult, since results now
belong to runs rather than directly to scenarios. This supports listing a scenario's run history,
newest first.

IsReady and ValidationIssuesJson are a persisted cache of the last validation, used for display in
scenario listings. They are written only when the verdict changes (issues are compared as values,
since jsonb reorders keys and reformats whitespace), so repeated /validate calls do not bump
UpdatedAt. Editing a scenario's network configuration clears IsReady.

Run creation decides readiness for itself. POST /api/scenarios/{id}/runs ignores the stored
IsReady and re-validates against current reference data at that moment:

- If the scenario passes, the run is created - even if it was never validated before, or its
  stored IsReady is false.
- If it fails, the request is rejected with 409, even if its stored IsReady is true (for example
  because a plant's capacity was reduced after the scenario was last validated).

Either way the stored IsReady and ValidationIssuesJson are refreshed by that validation.

Known limitation: QualityProfile.ConstraintMin and ConstraintMax are non-nullable decimals, so an
unset profile is stored as 0/0 and passes validation (0 ≤ 0). Rejecting it would mean making both
columns nullable and requiring at least one, which needs a migration; deferred.

Open items:
- QualityProfile is modelled as a standalone named limit, not linked to a specific Plant (see the
  Sprint 3 reference-data note above) — still pending confirmation.

## WaterSource.Type — source_type vocabulary (Sprint 3, resolved)

WaterSource.Type is a free-text column with no database check constraint, same treatment as
OptimisationRun.WorkflowStatus and SolverStatus above: the allowed values are documented here,
not enforced at the schema level, so the contract can grow without a migration each time.

Agreed values (lowercase, mirroring the MILP model output contract's source_type exactly):

* reservoir
* river
* groundwater

"Surface" was never a fourth value — it's the taxonomy category one level up that contains both
reservoir and river, so it was never a sibling of groundwater. The two seeded rows are now
reservoir ("Reservoir A") and groundwater ("Bore Well 1"); no fourth type is needed.

If a genuine new source type comes up (e.g. desalination, recycled water), it goes into the MILP
contract first and reaches this project from there — this project mirrors the contract's vocabulary,
it does not extend it independently.

Existing rows are normalised by the NormaliseWaterSourceTypes migration, which Program.cs applies
on startup (Surface → reservoir, any casing of reservoir/river/groundwater → lowercase). No manual
step is needed.
