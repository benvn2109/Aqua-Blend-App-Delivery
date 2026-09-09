# Database Setup and Migrations

## Applying migrations locally

1. Ensure PostgreSQL 17 is running and the aquablend database exists.
2. Set your connection string via user-secrets (see main README).
3. From Backend/, run: dotnet ef database update
4. Verify: psql -U postgres -d aquablend -c "\\dt"

## Creating a new migration

Run: dotnet ef migrations add MigrationName --project AquaBlend.Api.csproj

Review the generated file in Migrations/ before applying — check column types and any foreign key
delete behaviour match intent (EF Core defaults foreign keys to Cascade, which is not always correct).

## Schema notes

* OptimisationResult.ResultJson stores the full MILP model output contract as PostgreSQL jsonb.
* OptimisationResult.TotalCost / Currency are nullable — non-OPTIMAL solves omit the objective
block entirely in the source contract, so there is nothing to extract. Do not default to 0.
* OptimisationResult to Scenario foreign key uses ON DELETE RESTRICT, not cascade — a Scenario with
existing results cannot be deleted without explicitly handling its results first.
* Scenario.ExternalId resolves the JSON contract's scenario\_id string on ingest.
POST /api/optimisation-results must reject with 400 if no Scenario matches — never auto-create.



**Scenario.ExternalId nullability (fixed)**



ExternalId was originally NOT NULL with a default of empty string, which caused a unique-index

violation on any database with more than one Scenario row (surfaced as a 500 on the second

POST /api/scenarios call). It is now nullable, and the migration automatically converts any

existing blank ExternalId values to NULL. No manual backfill or database recreation is needed —

just apply the migration.



## OptimisationRun and the RunId re-pointing (Sprint 3)

OptimisationResult now belongs to an OptimisationRun rather than directly to a Scenario, since the
AI team pushes results against a run rather than the backend calling a solver directly. A run is
created via POST /api/scenarios/{id}/runs with WorkflowStatus starting at queued, and results are
posted against that run's id.

Schema:

* OptimisationRun: ScenarioId, WorkflowStatus (draft, ready, queued, solving, solved, analysing,
completed), SolverStatus (nullable until solved: OPTIMAL, INFEASIBLE, UNBOUNDED, TIME\_LIMIT, ERROR),
ScenarioSnapshotJson (jsonb, intended to capture the network configuration at run-creation time),
automatic CreatedAt/UpdatedAt.
* OptimisationResult.RunId is now required and unique (one result per run).
* OptimisationResult.ScenarioId is now nullable and kept temporarily for backward compatibility
with the Sprint 2 endpoints during the transition. It should be removed once those endpoints
are fully migrated to look up by RunId instead.

The migration backfills existing OptimisationResult rows automatically: for each result missing a
RunId, it creates a synthetic OptimisationRun (WorkflowStatus completed, SolverStatus copied from
the result's own Status, an empty ScenarioSnapshotJson placeholder) and points the result at it.
No manual database changes are needed, just apply the migration.

Open items, not yet resolved:

* ScenarioSnapshotJson is currently a placeholder ("{}") in seed data. The real snapshot should be
populated by whichever endpoint creates a run.
* SolverStatus casing: the MILP contract emits uppercase values (OPTIMAL, INFEASIBLE, etc.), while
an earlier architecture document used lowercase. Stored here exactly as the contract sends it;
any casing translation for consumers should happen at the API layer, not the database.

## Reference-data entities (Sprint 3)

Added five new reference-data entities that back the frontend's scenario builder:
Plant, DemandZone, SourcePlantLink, PlantZoneLink, QualityProfile. All are keyed by an internal
Id, with Plant and DemandZone also carrying an ExternalId (nullable, unique) to align with the
MILP contract's plant\_id/zone\_id where applicable.

WaterSource was expanded from 2 fields to include availability status, withdrawal bounds,
activation cost, cost per ML, provenance flags, and a model-ready flag, matching the source
fields described in the MILP model output contract. WaterSource.ExternalId is nullable for the
same reason as Scenario.ExternalId: existing rows have no natural external identifier to backfill,
and NULL avoids the unique-index collision that a blank-string default would cause.

Open item: QualityProfile is currently modelled as a standalone named limit (e.g. "Standard
Drinking Water") rather than linked directly to a Plant, per the MILP contract's own note that
quality limits are global rather than per-plant. This may need revisiting once the frontend's
exact GET /api/quality-profiles shape is confirmed with Ashwitha and Pavan.

