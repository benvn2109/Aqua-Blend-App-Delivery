using AquaBlend.Data;
using AquaBlend.DTOs.OptimisationResults;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AquaBlend.Services;

public class OptimisationResultService
{
    private readonly AquaBlendDbContext _context;

    public OptimisationResultService(AquaBlendDbContext context)
    {
        _context = context;
    }

    public async Task<List<OptimisationResultSummaryDto>> GetAllAsync()
    {
        return await _context.OptimisationResults
            .AsNoTracking()
            .OrderByDescending(r => r.ReceivedAt)
            .Select(r => new OptimisationResultSummaryDto
            {
                Id = r.Id,
                ScenarioId = r.ScenarioId,
                Status = r.Status,
                SolvedAt = r.SolvedAt,
                ReceivedAt = r.ReceivedAt,
                ContractVersion = r.ContractVersion,
                TotalCost = r.TotalCost,
                Currency = r.Currency,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<List<OptimisationResultSummaryDto>> GetByScenarioAsync(int scenarioId)
    {
        return await _context.OptimisationResults
            .AsNoTracking()
            .Where(r => r.ScenarioId == scenarioId)
            .OrderByDescending(r => r.ReceivedAt)
            .Select(r => new OptimisationResultSummaryDto
            {
                Id = r.Id,
                ScenarioId = r.ScenarioId,
                Status = r.Status,
                SolvedAt = r.SolvedAt,
                ReceivedAt = r.ReceivedAt,
                ContractVersion = r.ContractVersion,
                TotalCost = r.TotalCost,
                Currency = r.Currency,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<OptimisationResultResponseDto?> GetByIdAsync(int id)
    {
        var result = await _context.OptimisationResults
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (result is null)
            return null;

        using var document = JsonDocument.Parse(result.ResultJson);

        return new OptimisationResultResponseDto
        {
            Id = result.Id,
            ScenarioId = result.ScenarioId,
            Status = result.Status,
            SolvedAt = result.SolvedAt,
            ReceivedAt = result.ReceivedAt,
            ContractVersion = result.ContractVersion,
            ResultJson = document.RootElement.Clone(),
            TotalCost = result.TotalCost,
            Currency = result.Currency,
            CreatedAt = result.CreatedAt,
            UpdatedAt = result.UpdatedAt
        };
    }
}
