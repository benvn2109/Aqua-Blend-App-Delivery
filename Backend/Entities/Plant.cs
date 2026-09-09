namespace AquaBlend.Entities;

public class Plant
{
    public int Id { get; set; }

    public string ExternalId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal MaximumProcessingCapacityMlPerDay { get; set; }
    public decimal TreatmentCostPerMl { get; set; }

    public decimal? ActivationCost { get; set; }

    public bool IsModelReady { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
