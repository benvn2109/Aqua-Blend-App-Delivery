using System.ComponentModel.DataAnnotations;

namespace AquaBlend.DTOs.WaterSources;

public class CreateWaterSourceDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;
}
