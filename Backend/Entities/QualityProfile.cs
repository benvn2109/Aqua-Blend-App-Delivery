namespace AquaBlend.Entities;

public class QualityProfile
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Parameter { get; set; } = string.Empty; // e.g. "pH", "turbidity"
    public string Unit { get; set; } = string.Empty;

    public decimal ConstraintMin { get; set; }
    public decimal ConstraintMax { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}