using AquaBlend.Data;
using AquaBlend.DTOs.Runs;
using AquaBlend.DTOs.OptimisationResults;
using AquaBlend.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AquaBlend.Services;

public class RunService
{
    private readonly AquaBlendDbContext _context;
    private readonly ScenarioValidationService _validationService;

    public RunService(
        AquaBlendDbContext context,
        ScenarioValidationService validationService)
    {
        _context = context;
        _validationService = validationService;
    }

    public async Task<RunResponseDto?> CreateAsync(int scenarioId)
    {
        // Re-validate rather than trusting the stored IsReady: reference data
        // (capacities, demand, links) can change after the scenario was last
        // validated. Stored IsReady is a display cache for scenario listings.
        var validation = await _validationService.ValidateAsync(scenarioId);

        if (!validation.Found)
            return null;

        if (!validation.IsReady)
            throw new ScenarioNotReadyException(
                "Scenario must be validated and ready before a run can be created.");

        var scenario = await _context.Scenarios
            .FirstAsync(s => s.Id == scenarioId);

        var run = new OptimisationRun
        {
            ScenarioId = scenario.Id,

            // A newly submitted optimisation is ready to enter the
            // queued state. Run-status ownership can advance it later.
            WorkflowStatus = "queued",

            SolverStatus = null,

            // Preserve the exact scenario configuration used for this run.
            ScenarioSnapshotJson = scenario.NetworkConfigJson
        };

        _context.OptimisationRuns.Add(run);
        await _context.SaveChangesAsync();

        return Map(run);
    }

    public async Task<List<RunResponseDto>> GetByScenarioAsync(int scenarioId)
    {
        return await _context.OptimisationRuns
            .AsNoTracking()
            .Where(r => r.ScenarioId == scenarioId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RunResponseDto
            {
                Id = r.Id,
                ScenarioId = r.ScenarioId,
                WorkflowStatus = r.WorkflowStatus,
                SolverStatus = r.SolverStatus,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<RunResponseDto?> GetByIdAsync(int runId)
    {
        var run = await _context.OptimisationRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == runId);

        return run is null ? null : Map(run);
    }

    public async Task<OptimisationResultResponseDto?> GetResultAsync(int runId)
    {
        var result = await _context.OptimisationResults
            .AsNoTracking()
            .Include(r => r.Run)
            .FirstOrDefaultAsync(r => r.RunId == runId);

        if (result is null)
            return null;

        using var document = JsonDocument.Parse(result.ResultJson);

        return new OptimisationResultResponseDto
        {
            Id = result.Id,
            ScenarioId = result.Run!.ScenarioId,
            RunId = result.RunId,
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

    private static RunResponseDto Map(OptimisationRun run)
    {
        return new RunResponseDto
        {
            Id = run.Id,
            ScenarioId = run.ScenarioId,
            WorkflowStatus = run.WorkflowStatus,
            SolverStatus = run.SolverStatus,
            CreatedAt = run.CreatedAt,
            UpdatedAt = run.UpdatedAt
        };
    }
}

public sealed class ScenarioNotReadyException : Exception
{
    public ScenarioNotReadyException(string message)
        : base(message)
    {
    }
}