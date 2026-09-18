namespace AquaBlend.Entities;

public class WaterSource
{
    public int Id { get; set; }

    public string? ExternalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;

    public string AvailabilityStatus { get; set; } = "available"; // e.g. available, unavailable

    public decimal MinWithdrawalMlPerDay { get; set; }
    public decimal MaxWithdrawalMlPerDay { get; set; }

    public decimal? ActivationCost { get; set; } // F_s, structurally 0 per contract's known gaps
    public decimal CostPerMl { get; set; }

    public decimal? QualityAlkalinity { get; set; } // extend with more parameters as needed

    public bool HasEstimatedValues { get; set; }
    public string AvailabilityOrigin { get; set; } = "database"; // "database" or "scenario_override"

    public bool IsModelReady { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}