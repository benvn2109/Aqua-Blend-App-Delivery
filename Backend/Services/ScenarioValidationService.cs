using System.Text.Json;
using AquaBlend.Data;
using AquaBlend.Entities;
using Microsoft.EntityFrameworkCore;

namespace AquaBlend.Services;

public class ScenarioValidationService
{
    private readonly AquaBlendDbContext _context;

    public ScenarioValidationService(AquaBlendDbContext context)
    {
        _context = context;
    }

    public async Task<ScenarioValidationResult> ValidateAsync(int scenarioId)
    {
        var scenario = await _context.Scenarios
            .FirstOrDefaultAsync(s => s.Id == scenarioId);

        if (scenario is null)
            return ScenarioValidationResult.NotFound();

        var issues = new List<ScenarioValidationIssue>();

        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(scenario.NetworkConfigJson);
        }
        catch (JsonException)
        {
            issues.Add(new(
                "invalid_network_config",
                "Network configuration is not valid JSON.",
                "$"));

            return await SaveResultAsync(scenario, issues);
        }

        using (document)
        {
            var root = document.RootElement;

            // ---------------------------------------------------------
            // Rule 1: At least one source, plant and demand zone selected
            // ---------------------------------------------------------

            var sourceIds = ReadSelectedIds(
                root,
                "sources",
                "source_id",
                "$.sources",
                issues,
                "source");

            var plantIds = ReadSelectedIds(
                root,
                "plants",
                "plant_id",
                "$.plants",
                issues,
                "plant");

            var demandZoneContainer = GetDemandZoneContainer(root);

            var zoneIds = ReadSelectedIds(
                demandZoneContainer,
                "demand_zones",
                "zone_id",
                "$.network.demand_zones",
                issues,
                "demand zone");

            if (sourceIds.Count == 0)
            {
                issues.Add(new(
                    "sources_required",
                    "At least one source must be selected.",
                    "$.sources"));
            }

            if (plantIds.Count == 0)
            {
                issues.Add(new(
                    "plants_required",
                    "At least one plant must be selected.",
                    "$.plants"));
            }

            if (zoneIds.Count == 0)
            {
                issues.Add(new(
                    "demand_zones_required",
                    "At least one demand zone must be selected.",
                    "$.network.demand_zones"));
            }

            // ---------------------------------------------------------
            // Load referenced reference-data entities
            // ---------------------------------------------------------

            var sources = await _context.WaterSources
                .AsNoTracking()
                .Where(s =>
                    s.ExternalId != null &&
                    sourceIds.Contains(s.ExternalId))
                .ToListAsync();

            var plants = await _context.Plants
                .AsNoTracking()
                .Where(p =>
                    p.ExternalId != null &&
                    plantIds.Contains(p.ExternalId))
                .ToListAsync();

            var zones = await _context.DemandZones
                .AsNoTracking()
                .Where(z =>
                    z.ExternalId != null &&
                    zoneIds.Contains(z.ExternalId))
                .ToListAsync();

            // ---------------------------------------------------------
            // Rule 4: Every referenced entity exists and is usable
            // ---------------------------------------------------------

            ValidateReferencedSources(
                sourceIds,
                sources,
                issues);

            ValidateReferencedPlants(
                plantIds,
                plants,
                issues);

            ValidateReferencedZones(
                zoneIds,
                zones,
                issues);

            // ---------------------------------------------------------
            // Rule 5: Quality profile selected and valid
            // ---------------------------------------------------------

            var qualityProfileId = GetQualityProfileId(root);

            if (!qualityProfileId.HasValue)
            {
                issues.Add(new(
                    "quality_profile_required",
                    "A quality profile must be selected.",
                    "$.quality_profile_id"));
            }
            else
            {
                var qualityProfile = await _context.QualityProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        q => q.Id == qualityProfileId.Value);

                if (qualityProfile is null)
                {
                    issues.Add(new(
                        "quality_profile_missing",
                        $"Quality profile '{qualityProfileId.Value}' does not exist in the database.",
                        "$.quality_profile_id"));
                }
                else
                {
                    ValidateQualityProfile(
                        qualityProfile,
                        issues);
                }
            }

            // ---------------------------------------------------------
            // Rule 2: Every selected zone must be reachable from
            //         a selected source through a selected plant
            // ---------------------------------------------------------

            await ValidateReachabilityAsync(
                root,
                sourceIds,
                plantIds,
                zoneIds,
                sources,
                plants,
                zones,
                issues);

            // ---------------------------------------------------------
            // Rule 3: Total plant capacity >= total demand
            // ---------------------------------------------------------

            ValidateCapacity(
                plants,
                zones,
                issues);
        }

        return await SaveResultAsync(
            scenario,
            issues);
    }

    private async Task<ScenarioValidationResult> SaveResultAsync(
        Scenario scenario,
        List<ScenarioValidationIssue> issues)
    {
        // IsReady is calculated here only.
        // It is never accepted from the client request.
        var isReady = issues.Count == 0;

        // Only write when the verdict changed: every write bumps UpdatedAt,
        // and the frontend validates on each edit, so unconditional saves
        // would flood the /changes feed with scenarios that did not change.
        if (scenario.IsReady != isReady ||
            !StoredIssuesEqual(scenario.ValidationIssuesJson, issues))
        {
            scenario.IsReady = isReady;

            scenario.ValidationIssuesJson =
                JsonSerializer.Serialize(issues);

            await _context.SaveChangesAsync();
        }

        return new ScenarioValidationResult(
            true,
            isReady,
            issues);
    }

    // Compared as values, not text: Postgres jsonb reorders keys and
    // reformats whitespace, so the stored string never matches a fresh
    // JsonSerializer.Serialize even when the issues are identical.
    private static bool StoredIssuesEqual(
        string storedJson,
        List<ScenarioValidationIssue> issues)
    {
        try
        {
            var stored = JsonSerializer.Deserialize<List<ScenarioValidationIssue>>(storedJson);

            return stored is not null &&
                   stored.SequenceEqual(issues);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    // ================================================================
    // Selection parsing
    // ================================================================

    private static List<string> ReadSelectedIds(
        JsonElement container,
        string arrayName,
        string idProperty,
        string path,
        List<ScenarioValidationIssue> issues,
        string entityName)
    {
        var ids = new List<string>();

        if (container.ValueKind != JsonValueKind.Object ||
            !container.TryGetProperty(
                arrayName,
                out var array) ||
            array.ValueKind != JsonValueKind.Array)
        {
            return ids;
        }

        foreach (var item in array.EnumerateArray())
        {
            if (!IsSelected(item))
                continue;

            var id = GetString(
                item,
                idProperty);

            if (string.IsNullOrWhiteSpace(id))
            {
                issues.Add(new(
                    $"{entityName.Replace(" ", "_")}_id_missing",
                    $"Selected {entityName} is missing {idProperty}.",
                    path));

                continue;
            }

            if (!ids.Contains(
                    id,
                    StringComparer.OrdinalIgnoreCase))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    private static bool IsSelected(JsonElement element)
    {
        // If enabled is omitted, treat the item as selected.
        return !element.TryGetProperty(
                   "enabled",
                   out var enabled) ||
               enabled.ValueKind == JsonValueKind.True;
    }

    // ================================================================
    // Rule 2 - Reachability
    // ================================================================

    private async Task ValidateReachabilityAsync(
        JsonElement root,
        IReadOnlyCollection<string> sourceIds,
        IReadOnlyCollection<string> plantIds,
        IReadOnlyCollection<string> zoneIds,
        IReadOnlyCollection<WaterSource> sourceEntities,
        IReadOnlyCollection<Plant> plantEntities,
        IReadOnlyCollection<DemandZone> zoneEntities,
        List<ScenarioValidationIssue> issues)
    {
        if (sourceIds.Count == 0 ||
            plantIds.Count == 0 ||
            zoneIds.Count == 0)
        {
            return;
        }

        // sourceEntities/plantEntities/zoneEntities are the rows ValidateAsync
        // already loaded for the selected ids - not re-queried here.
        var sourceIdSet =
            sourceEntities
                .Select(s => s.Id)
                .ToHashSet();

        var plantIdSet =
            plantEntities
                .Select(p => p.Id)
                .ToHashSet();

        var zoneIdSet =
            zoneEntities
                .Select(z => z.Id)
                .ToHashSet();

        var explicitLinks =
            GetExplicitLinks(root);

        HashSet<(int SourceId, int PlantId)>
            sourcePlantPairs;

        HashSet<(int PlantId, int ZoneId)>
            plantZonePairs;

        if (explicitLinks.HasLinks)
        {
            sourcePlantPairs = new();
            plantZonePairs = new();

            foreach (var link in
                     explicitLinks.SourcePlantLinks)
            {
                var source =
                    sourceEntities.FirstOrDefault(s =>
                        string.Equals(
                            s.ExternalId,
                            link.FromId,
                            StringComparison.OrdinalIgnoreCase));

                var plant =
                    plantEntities.FirstOrDefault(p =>
                        string.Equals(
                            p.ExternalId,
                            link.ToId,
                            StringComparison.OrdinalIgnoreCase));

                if (source is null ||
                    plant is null)
                {
                    issues.Add(new(
                        "network_link_reference_missing",
                        $"Source-to-plant link references a source or plant that is not selected or does not exist: '{link.FromId}' -> '{link.ToId}'.",
                        link.Path));

                    continue;
                }

                sourcePlantPairs.Add(
                    (source.Id, plant.Id));
            }

            foreach (var link in
                     explicitLinks.PlantZoneLinks)
            {
                var plant =
                    plantEntities.FirstOrDefault(p =>
                        string.Equals(
                            p.ExternalId,
                            link.FromId,
                            StringComparison.OrdinalIgnoreCase));

                var zone =
                    zoneEntities.FirstOrDefault(z =>
                        string.Equals(
                            z.ExternalId,
                            link.ToId,
                            StringComparison.OrdinalIgnoreCase));

                if (plant is null ||
                    zone is null)
                {
                    issues.Add(new(
                        "network_link_reference_missing",
                        $"Plant-to-zone link references a plant or demand zone that is not selected or does not exist: '{link.FromId}' -> '{link.ToId}'.",
                        link.Path));

                    continue;
                }

                plantZonePairs.Add(
                    (plant.Id, zone.Id));
            }
        }
        else
        {
            // If the scenario does not explicitly provide links,
            // use the active reference-data topology.
            var sourcePlantLinks =
                await _context.SourcePlantLinks
                    .AsNoTracking()
                    .Where(l =>
                        l.IsActive &&
                        sourceIdSet.Contains(
                            l.WaterSourceId) &&
                        plantIdSet.Contains(
                            l.PlantId))
                    .Select(l => new
                    {
                        l.WaterSourceId,
                        l.PlantId
                    })
                    .ToListAsync();

            var plantZoneLinks =
                await _context.PlantZoneLinks
                    .AsNoTracking()
                    .Where(l =>
                        l.IsActive &&
                        plantIdSet.Contains(
                            l.PlantId) &&
                        zoneIdSet.Contains(
                            l.DemandZoneId))
                    .Select(l => new
                    {
                        l.PlantId,
                        l.DemandZoneId
                    })
                    .ToListAsync();

            sourcePlantPairs =
                sourcePlantLinks
                    .Select(l =>
                        (l.WaterSourceId, l.PlantId))
                    .ToHashSet();

            plantZonePairs =
                plantZoneLinks
                    .Select(l =>
                        (l.PlantId, l.DemandZoneId))
                    .ToHashSet();
        }

        foreach (var zone in zoneEntities)
        {
            var reachable =
                plantZonePairs
                    .Where(pz =>
                        pz.ZoneId == zone.Id)
                    .Any(pz =>
                        sourcePlantPairs.Any(sp =>
                            sp.PlantId == pz.PlantId));

            if (!reachable)
            {
                issues.Add(new(
                    "demand_zone_unreachable",
                    $"Demand zone '{zone.ExternalId}' is not reachable from any selected source through a selected plant.",
                    "$.network.demand_zones"));
            }
        }
    }

    // ================================================================
    // Rule 3 - Capacity
    // ================================================================

    private static void ValidateCapacity(
        IReadOnlyCollection<Plant> plants,
        IReadOnlyCollection<DemandZone> zones,
        List<ScenarioValidationIssue> issues)
    {
        if (plants.Count == 0 ||
            zones.Count == 0)
        {
            return;
        }

        var totalCapacity =
            plants.Sum(p =>
                p.MaximumProcessingCapacityMlPerDay);

        var totalDemand =
            zones.Sum(z =>
                z.DemandMlPerDay);

        if (totalCapacity < totalDemand)
        {
            issues.Add(new(
                "plant_capacity_insufficient",
                $"Selected plant capacity ({totalCapacity:0.##} ML/day) is less than total demand ({totalDemand:0.##} ML/day).",
                "$.plants"));
        }
    }

    // ================================================================
    // Rule 4 - Sources
    // ================================================================

    private static void ValidateReferencedSources(
        IReadOnlyCollection<string> selectedIds,
        IReadOnlyCollection<WaterSource> sources,
        List<ScenarioValidationIssue> issues)
    {
        var found =
            sources
                .Where(s =>
                    !string.IsNullOrWhiteSpace(
                        s.ExternalId))
                .Select(s =>
                    s.ExternalId!)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        foreach (var id in
                 selectedIds.Where(id =>
                     !found.Contains(id)))
        {
            issues.Add(new(
                "source_missing_from_database",
                $"Source '{id}' does not exist in the database.",
                "$.sources"));
        }

        foreach (var source in sources)
        {
            if (!source.IsModelReady)
            {
                issues.Add(new(
                    "source_not_model_ready",
                    $"Source '{source.ExternalId}' is not model-ready.",
                    "$.sources"));
            }
        }
    }

    // ================================================================
    // Rule 4 - Plants
    // ================================================================

    private static void ValidateReferencedPlants(
        IReadOnlyCollection<string> selectedIds,
        IReadOnlyCollection<Plant> plants,
        List<ScenarioValidationIssue> issues)
    {
        var found =
            plants
                .Where(p =>
                    !string.IsNullOrWhiteSpace(
                        p.ExternalId))
                .Select(p =>
                    p.ExternalId!)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        foreach (var id in
                 selectedIds.Where(id =>
                     !found.Contains(id)))
        {
            issues.Add(new(
                "plant_missing_from_database",
                $"Plant '{id}' does not exist in the database.",
                "$.plants"));
        }

        foreach (var plant in plants)
        {
            if (!plant.IsModelReady)
            {
                issues.Add(new(
                    "plant_not_model_ready",
                    $"Plant '{plant.ExternalId}' is not model-ready.",
                    "$.plants"));
            }
        }
    }

    // ================================================================
    // Rule 4 - Demand zones
    // ================================================================

    private static void ValidateReferencedZones(
        IReadOnlyCollection<string> selectedIds,
        IReadOnlyCollection<DemandZone> zones,
        List<ScenarioValidationIssue> issues)
    {
        var found =
            zones
                .Where(z =>
                    !string.IsNullOrWhiteSpace(
                        z.ExternalId))
                .Select(z =>
                    z.ExternalId!)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        foreach (var id in
                 selectedIds.Where(id =>
                     !found.Contains(id)))
        {
            issues.Add(new(
                "demand_zone_missing_from_database",
                $"Demand zone '{id}' does not exist in the database.",
                "$.network.demand_zones"));
        }

        foreach (var zone in zones)
        {
            if (zone.DemandMlPerDay <= 0)
            {
                issues.Add(new(
                    "demand_missing",
                    $"Demand zone '{zone.ExternalId}' has no valid daily demand value. DemandMlPerDay must be greater than zero.",
                    "$.network.demand_zones"));
            }
        }
    }

    // ================================================================
    // Rule 4 / 5 - Quality profile
    // ================================================================

    private static void ValidateQualityProfile(
        QualityProfile profile,
        List<ScenarioValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(
                profile.Parameter))
        {
            issues.Add(new(
                "quality_profile_parameter_missing",
                $"Quality profile '{profile.Id}' is missing Parameter.",
                "$.quality_profile_id"));
        }

        if (string.IsNullOrWhiteSpace(
                profile.Unit))
        {
            issues.Add(new(
                "quality_profile_unit_missing",
                $"Quality profile '{profile.Id}' is missing Unit.",
                "$.quality_profile_id"));
        }

        if (profile.ConstraintMin >
            profile.ConstraintMax)
        {
            issues.Add(new(
                "quality_profile_invalid_range",
                $"Quality profile '{profile.Id}' has ConstraintMin greater than ConstraintMax.",
                "$.quality_profile_id"));
        }
    }

    // ================================================================
    // JSON helpers
    // ================================================================

    private static JsonElement GetDemandZoneContainer(
        JsonElement root)
    {
        return root.TryGetProperty(
                   "network",
                   out var network) &&
               network.ValueKind ==
                   JsonValueKind.Object
            ? network
            : root;
    }

    private static int? GetQualityProfileId(
        JsonElement root)
    {
        if (TryGetQualityProfileId(
                root,
                out var id))
        {
            return id;
        }

        if (root.TryGetProperty(
                "network",
                out var network) &&
            network.ValueKind ==
                JsonValueKind.Object &&
            TryGetQualityProfileId(
                network,
                out id))
        {
            return id;
        }

        return null;
    }

    private static bool TryGetQualityProfileId(
        JsonElement element,
        out int id)
    {
        id = 0;

        if (TryGetInt(
                element,
                "quality_profile_id",
                out id) &&
            id > 0)
        {
            return true;
        }

        if (!element.TryGetProperty(
                "quality_profile",
                out var profile))
        {
            return false;
        }

        if (profile.ValueKind ==
                JsonValueKind.Number &&
            profile.TryGetInt32(out id) &&
            id > 0)
        {
            return true;
        }

        if (profile.ValueKind ==
                JsonValueKind.String &&
            int.TryParse(
                profile.GetString(),
                out id) &&
            id > 0)
        {
            return true;
        }

        if (profile.ValueKind ==
            JsonValueKind.Object)
        {
            if (TryGetInt(
                    profile,
                    "id",
                    out id) &&
                id > 0)
            {
                return true;
            }

            if (TryGetInt(
                    profile,
                    "quality_profile_id",
                    out id) &&
                id > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetInt(
        JsonElement element,
        string propertyName,
        out int value)
    {
        value = 0;

        if (!element.TryGetProperty(
                propertyName,
                out var property))
        {
            return false;
        }

        if (property.ValueKind ==
            JsonValueKind.Number)
        {
            return property.TryGetInt32(
                out value);
        }

        return property.ValueKind ==
                   JsonValueKind.String &&
               int.TryParse(
                   property.GetString(),
                   out value);
    }

    private static string? GetString(
        JsonElement element,
        string propertyName)
    {
        return element.TryGetProperty(
                   propertyName,
                   out var value) &&
               value.ValueKind ==
                   JsonValueKind.String
            ? value.GetString()
            : null;
    }

    // ================================================================
    // Network links
    // ================================================================

    private static ExplicitLinks GetExplicitLinks(
        JsonElement root)
    {
        JsonElement links;

        if (root.TryGetProperty(
                "links",
                out var rootLinks) &&
            rootLinks.ValueKind ==
                JsonValueKind.Object)
        {
            links = rootLinks;
        }
        else if (root.TryGetProperty(
                     "network",
                     out var network) &&
                 network.ValueKind ==
                     JsonValueKind.Object &&
                 network.TryGetProperty(
                     "links",
                     out var networkLinks) &&
                 networkLinks.ValueKind ==
                     JsonValueKind.Object)
        {
            links = networkLinks;
        }
        else
        {
            return new ExplicitLinks(
                false,
                [],
                []);
        }

        var sourcePlantResult =
            ReadLinkArray(
                links,
                new[]
                {
                    "source_to_plant",
                    "source_plant",
                    "source_plant_links"
                },
                "source_id",
                "plant_id");

        var plantZoneResult =
            ReadLinkArray(
                links,
                new[]
                {
                    "plant_to_zone",
                    "plant_zone",
                    "plant_zone_links"
                },
                "plant_id",
                "zone_id");

        return new ExplicitLinks(
            sourcePlantResult.Present ||
            plantZoneResult.Present,
            sourcePlantResult.Links,
            plantZoneResult.Links);
    }

    private static LinkReadResult ReadLinkArray(
        JsonElement links,
        IEnumerable<string> propertyNames,
        string fromProperty,
        string toProperty)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!links.TryGetProperty(
                    propertyName,
                    out var array) ||
                array.ValueKind !=
                    JsonValueKind.Array)
            {
                continue;
            }

            var result =
                new List<ExplicitLink>();

            foreach (var item in
                     array.EnumerateArray())
            {
                if (!IsSelected(item))
                    continue;

                var fromId =
                    GetString(
                        item,
                        fromProperty);

                var toId =
                    GetString(
                        item,
                        toProperty);

                if (string.IsNullOrWhiteSpace(
                        fromId) ||
                    string.IsNullOrWhiteSpace(
                        toId))
                {
                    continue;
                }

                result.Add(new ExplicitLink(
                    fromId,
                    toId,
                    $"$.links.{propertyName}"));
            }

            return new LinkReadResult(
                true,
                result);
        }

        return new LinkReadResult(
            false,
            []);
    }

    private sealed record ExplicitLinks(
        bool HasLinks,
        List<ExplicitLink> SourcePlantLinks,
        List<ExplicitLink> PlantZoneLinks);

    private sealed record ExplicitLink(
        string FromId,
        string ToId,
        string Path);

    private sealed record LinkReadResult(
        bool Present,
        List<ExplicitLink> Links);
}

public record ScenarioValidationIssue(
    string Code,
    string Message,
    string Path);

public record ScenarioValidationResult(
    bool Found,
    bool IsReady,
    IReadOnlyList<ScenarioValidationIssue> ValidationIssues)
{
    public static ScenarioValidationResult NotFound() =>
        new(
            false,
            false,
            Array.Empty<ScenarioValidationIssue>());
}