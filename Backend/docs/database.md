# Database Setup and Migrations

## Applying migrations locally

1. Ensure PostgreSQL 17 is running and the aquablend database exists.
2. Set your connection string via user-secrets (see main README).
3. From Backend/, run: dotnet ef database update
4. Verify: psql -U postgres -d aquablend -c "\dt"

## Creating a new migration

Run: dotnet ef migrations add MigrationName --project AquaBlend.Api.csproj

Review the generated file in Migrations/ before applying — check column types and any foreign key
delete behaviour match intent (EF Core defaults foreign keys to Cascade, which is not always correct).

## Schema notes

- OptimisationResult.ResultJson stores the full MILP model output contract as PostgreSQL jsonb.
- OptimisationResult.TotalCost / Currency are nullable — non-OPTIMAL solves omit the objective
  block entirely in the source contract, so there is nothing to extract. Do not default to 0.
- OptimisationResult to Scenario foreign key uses ON DELETE RESTRICT, not cascade — a Scenario with
  existing results cannot be deleted without explicitly handling its results first.
- Scenario.ExternalId resolves the JSON contract's scenario_id string on ingest.
  POST /api/optimisation-results must reject with 400 if no Scenario matches — never auto-create.
