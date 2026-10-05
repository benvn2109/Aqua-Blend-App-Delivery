namespace AquaBlend.DTOs.ReferenceData;

// Controlled vocabulary for WaterSource.Type, aligned with the source_type
// values already used in the MILP model output contract (model_output_contract.json,
// sources.selected[].source_type / sources.unused[].source_type). Agreed with the
// team (see docs/database.md) - not enforced with a database check constraint,
// same treatment as OptimisationRun.WorkflowStatus/SolverStatus, so the contract
// can grow without a migration each time.
public static class SourceTypes
{
    public const string Reservoir = "reservoir";
    public const string River = "river";
    public const string Groundwater = "groundwater";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Reservoir,
        River,
        Groundwater
    };
}
