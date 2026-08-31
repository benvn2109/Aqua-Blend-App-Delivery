namespace AquaBlend.Entities;

public class Scenario
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? ExternalId { get; set; }

    public ICollection<OptimisationResult> OptimisationResults { get; set; } = new List<OptimisationResult>();
}
