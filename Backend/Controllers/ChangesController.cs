using AquaBlend.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;
using AquaBlend.Data;
using AquaBlend.DTOs;
using AquaBlend.DTOs.Changes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AquaBlend.Controllers;

[ApiController]
[Route("api/changes")]
public sealed class ChangesController : ControllerBase
{
    private readonly AquaBlendDbContext _context;

    public ChangesController(AquaBlendDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = AppPolicies.CanView)]
    public async Task<ActionResult<ChangesResponseDto>> GetChanges(
        [FromQuery] string? since,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(since))
        {
            return BadRequest(new
            {
                error = "The 'since' query parameter is required."
            });
        }

        if (!DateTimeOffset.TryParse(
                since,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsedSince))
        {
            return BadRequest(new
            {
                error = "The 'since' query parameter must be a valid ISO 8601 timestamp."
            });
        }

        if (parsedSince.Offset != TimeSpan.Zero)
        {
            return BadRequest(new
            {
                error = "The 'since' timestamp must use UTC."
            });
        }

        var sinceUtc = parsedSince.UtcDateTime;
        var serverTimestamp = DateTime.UtcNow;

        var waterSources = await _context.WaterSources
            .AsNoTracking()
            .Where(w =>
                w.CreatedAt > sinceUtc ||
                (w.UpdatedAt.HasValue && w.UpdatedAt.Value > sinceUtc))
            .ToListAsync(cancellationToken);

        var scenarios = await _context.Scenarios
            .AsNoTracking()
            .Where(s =>
                s.CreatedAt > sinceUtc ||
                (s.UpdatedAt.HasValue && s.UpdatedAt.Value > sinceUtc))
            .ToListAsync(cancellationToken);

        var optimisationResults = await _context.OptimisationResults
            .AsNoTracking()
            .Where(r =>
                r.CreatedAt > sinceUtc ||
                (r.UpdatedAt.HasValue && r.UpdatedAt.Value > sinceUtc))
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
            .ToListAsync(cancellationToken);

        return Ok(new ChangesResponseDto
        {
            RequestedSince = sinceUtc,
            ServerTimestamp = serverTimestamp,
            WaterSources = waterSources,
            Scenarios = scenarios,
            OptimisationResults = optimisationResults
        });
    }
}