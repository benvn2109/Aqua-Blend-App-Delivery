namespace AquaBlend.DTOs.ReferenceData;

public class SourcePlantLinkResponseDto
{
    public int Id { get; set; }
    public int WaterSourceId { get; set; }
    public int PlantId { get; set; }
    public bool IsActive { get; set; }
    public decimal MaximumCapacityMlPerDay { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
