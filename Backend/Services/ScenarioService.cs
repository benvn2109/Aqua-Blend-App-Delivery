using AquaBlend.Data;
using AquaBlend.DTOs.Scenarios;
using AquaBlend.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AquaBlend.Services;

public class ScenarioService
{
    private readonly AquaBlendDbContext _context;

    public ScenarioService(AquaBlendDbContext context)
    {
        _context = context;
    }

    public async Task<List<ScenarioResponseDto>> GetAllAsync()
    {
        var scenarios = await _context.Scenarios
            .AsNoTracking()
            .ToListAsync();

        return scenarios.Select(MapToResponse).ToList();
    }

    public async Task<ScenarioResponseDto?> GetByIdAsync(int id)
    {
        var scenario = await _context.Scenarios
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);

        return scenario is null ? null : MapToResponse(scenario);
    }

    public async Task<ScenarioResponseDto> CreateAsync(CreateScenarioDto dto)
    {
        var scenario = new Scenario
        {
            Name = dto.Name,
            Description = dto.Description,
            ExternalId = dto.ExternalId,
            NetworkConfigJson = dto.NetworkConfig.HasValue
                ? dto.NetworkConfig.Value.GetRawText()
                : "{}"
        };

        _context.Scenarios.Add(scenario);
        await _context.SaveChangesAsync();

        return MapToResponse(scenario);
    }

    public async Task<bool> UpdateAsync(int id, UpdateScenarioDto dto)
    {
        var scenario = await _context.Scenarios.FindAsync(id);

        if (scenario == null)
            return false;

        scenario.Name = dto.Name;
        scenario.Description = dto.Description;
        scenario.ExternalId = dto.ExternalId;

        if (dto.NetworkConfig.HasValue)
        {
            var networkConfigJson = dto.NetworkConfig.Value.GetRawText();

            // A changed configuration has not been validated, so it must not
            // keep a readiness verdict earned by the previous configuration.
            if (networkConfigJson != scenario.NetworkConfigJson)
            {
                scenario.NetworkConfigJson = networkConfigJson;
                scenario.IsReady = false;
            }
        }

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var scenario = await _context.Scenarios.FindAsync(id);

        if (scenario == null)
            return false;

        _context.Scenarios.Remove(scenario);
        await _context.SaveChangesAsync();

        return true;
    }

    private static ScenarioResponseDto MapToResponse(Scenario scenario)
    {
        return new ScenarioResponseDto
        {
            Id = scenario.Id,
            Name = scenario.Name,
            Description = scenario.Description,
            ExternalId = scenario.ExternalId,
            NetworkConfig = ParseJsonOrEmptyObject(scenario.NetworkConfigJson),
            IsReady = scenario.IsReady,
            ValidationIssues = ParseJsonOrEmptyArray(scenario.ValidationIssuesJson),
            CreatedAt = scenario.CreatedAt,
            UpdatedAt = scenario.UpdatedAt
        };
    }

    private static JsonElement ParseJsonOrEmptyObject(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return JsonDocument.Parse("{}").RootElement.Clone();
        }
    }

    private static JsonElement ParseJsonOrEmptyArray(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return JsonDocument.Parse("[]").RootElement.Clone();
        }
    }
}