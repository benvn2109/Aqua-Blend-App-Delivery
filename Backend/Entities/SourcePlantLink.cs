namespace AquaBlend.Entities;

public class SourcePlantLink
{
    public int Id { get; set; }

    public int WaterSourceId { get; set; }
    public WaterSource WaterSource { get; set; } = null!;

    public int PlantId { get; set; }
    public Plant Plant { get; set; } = null!;

    // Static network topology — is this link allowed to exist at all,
    // independent of any particular optimisation run's solved flow
    public bool IsActive { get; set; }

    // Static capacity ceiling for this link, distinct from a run's
    // solved flow_ml_per_day (which lives in the result JSON, not here)
    public decimal MaximumCapacityMlPerDay { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}