namespace AquaBlend.DTOs.ReferenceData;

public class DemandZoneResponseDto
{
    public int Id { get; set; }
    public string? ExternalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DemandMlPerDay { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
