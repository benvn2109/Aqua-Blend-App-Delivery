using AquaBlend.DTOs.OptimisationResults;
using AquaBlend.Entities;

namespace AquaBlend.DTOs;

public sealed class ChangesResponseDto
{
    public DateTime RequestedSince { get; init; }

    public DateTime ServerTimestamp { get; init; }

    public IReadOnlyList<WaterSource> WaterSources { get; init; }
        = Array.Empty<WaterSource>();

    public IReadOnlyList<Scenario> Scenarios { get; init; }
        = Array.Empty<Scenario>();

    public IReadOnlyList<OptimisationResultSummaryDto> OptimisationResults { get; init; }
        = Array.Empty<OptimisationResultSummaryDto>();
}