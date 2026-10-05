namespace AquaBlend.DTOs.ReferenceData;

// Mirrors the transfer_paths shape in the MILP model output contract
// (source_to_plant / plant_to_zone), so the two link kinds line up with
// what the frontend already expects from the results side.
public class NetworkLinksResponseDto
{
    public IReadOnlyList<SourcePlantLinkResponseDto> SourceToPlant { get; init; }
        = Array.Empty<SourcePlantLinkResponseDto>();

    public IReadOnlyList<PlantZoneLinkResponseDto> PlantToZone { get; init; }
        = Array.Empty<PlantZoneLinkResponseDto>();
}
