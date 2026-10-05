namespace AquaBlend.DTOs.ReferenceData;

public class PlantZoneLinkResponseDto
{
    public int Id { get; set; }
    public int PlantId { get; set; }
    public int DemandZoneId { get; set; }
    public bool IsActive { get; set; }
    public decimal MaximumCapacityMlPerDay { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
