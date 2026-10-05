using AquaBlend.Data;
using AquaBlend.DTOs.ReferenceData;
using Microsoft.EntityFrameworkCore;

namespace AquaBlend.Services;

public class ReferenceDataService
{
    private readonly AquaBlendDbContext _context;

    public ReferenceDataService(AquaBlendDbContext context)
    {
        _context = context;
    }

    public async Task<List<SourceResponseDto>> GetSourcesAsync()
    {
        return await _context.WaterSources
            .AsNoTracking()
            .Select(w => new SourceResponseDto
            {
                Id = w.Id,
                ExternalId = w.ExternalId,
                Name = w.Name,
                Type = w.Type,
                AvailabilityStatus = w.AvailabilityStatus,
                MinWithdrawalMlPerDay = w.MinWithdrawalMlPerDay,
                MaxWithdrawalMlPerDay = w.MaxWithdrawalMlPerDay,
                ActivationCost = w.ActivationCost,
                CostPerMl = w.CostPerMl,
                QualityAlkalinity = w.QualityAlkalinity,
                HasEstimatedValues = w.HasEstimatedValues,
                AvailabilityOrigin = w.AvailabilityOrigin,
                IsModelReady = w.IsModelReady,
                CreatedAt = w.CreatedAt,
                UpdatedAt = w.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<List<PlantResponseDto>> GetPlantsAsync()
    {
        return await _context.Plants
            .AsNoTracking()
            .Select(p => new PlantResponseDto
            {
                Id = p.Id,
                ExternalId = p.ExternalId,
                Name = p.Name,
                MaximumProcessingCapacityMlPerDay = p.MaximumProcessingCapacityMlPerDay,
                TreatmentCostPerMl = p.TreatmentCostPerMl,
                ActivationCost = p.ActivationCost,
                IsModelReady = p.IsModelReady,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<List<DemandZoneResponseDto>> GetDemandZonesAsync()
    {
        return await _context.DemandZones
            .AsNoTracking()
            .Select(z => new DemandZoneResponseDto
            {
                Id = z.Id,
                ExternalId = z.ExternalId,
                Name = z.Name,
                DemandMlPerDay = z.DemandMlPerDay,
                CreatedAt = z.CreatedAt,
                UpdatedAt = z.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<List<QualityProfileResponseDto>> GetQualityProfilesAsync()
    {
        return await _context.QualityProfiles
            .AsNoTracking()
            .Select(q => new QualityProfileResponseDto
            {
                Id = q.Id,
                Name = q.Name,
                Parameter = q.Parameter,
                Unit = q.Unit,
                ConstraintMin = q.ConstraintMin,
                ConstraintMax = q.ConstraintMax,
                CreatedAt = q.CreatedAt,
                UpdatedAt = q.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<NetworkLinksResponseDto> GetNetworkLinksAsync()
    {
        var sourceToPlant = await _context.SourcePlantLinks
            .AsNoTracking()
            .Select(l => new SourcePlantLinkResponseDto
            {
                Id = l.Id,
                WaterSourceId = l.WaterSourceId,
                PlantId = l.PlantId,
                IsActive = l.IsActive,
                MaximumCapacityMlPerDay = l.MaximumCapacityMlPerDay,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt
            })
            .ToListAsync();

        var plantToZone = await _context.PlantZoneLinks
            .AsNoTracking()
            .Select(l => new PlantZoneLinkResponseDto
            {
                Id = l.Id,
                PlantId = l.PlantId,
                DemandZoneId = l.DemandZoneId,
                IsActive = l.IsActive,
                MaximumCapacityMlPerDay = l.MaximumCapacityMlPerDay,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt
            })
            .ToListAsync();

        return new NetworkLinksResponseDto
        {
            SourceToPlant = sourceToPlant,
            PlantToZone = plantToZone
        };
    }
}
