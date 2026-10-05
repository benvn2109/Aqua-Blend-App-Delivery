namespace AquaBlend.DTOs.ReferenceData;

public class SourceResponseDto
{
    public int Id { get; set; }
    public string? ExternalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;

    public string AvailabilityStatus { get; set; } = string.Empty;

    public decimal MinWithdrawalMlPerDay { get; set; }
    public decimal MaxWithdrawalMlPerDay { get; set; }

    public decimal? ActivationCost { get; set; }
    public decimal CostPerMl { get; set; }

    public decimal? QualityAlkalinity { get; set; }

    public bool HasEstimatedValues { get; set; }
    public string AvailabilityOrigin { get; set; } = string.Empty;

    public bool IsModelReady { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
