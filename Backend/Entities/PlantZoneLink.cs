namespace AquaBlend.Entities;

public class PlantZoneLink
{
    public int Id { get; set; }

    public int PlantId { get; set; }
    public Plant Plant { get; set; } = null!;

    public int DemandZoneId { get; set; }
    public DemandZone DemandZone { get; set; } = null!;

    public bool IsActive { get; set; }
    public decimal MaximumCapacityMlPerDay { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}