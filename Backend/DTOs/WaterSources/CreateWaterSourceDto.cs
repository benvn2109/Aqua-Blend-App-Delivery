using System.ComponentModel.DataAnnotations;

namespace AquaBlend.DTOs.WaterSources;

public class CreateWaterSourceDto
{
    [Required]
    [RegularExpression(@".*\S.*", ErrorMessage = "Name cannot contain only whitespace.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@".*\S.*", ErrorMessage = "Type cannot contain only whitespace.")]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;
}
