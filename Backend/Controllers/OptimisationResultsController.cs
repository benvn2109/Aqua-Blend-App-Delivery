using AquaBlend.DTOs.OptimisationResults;
using AquaBlend.Services;
using Microsoft.AspNetCore.Mvc;
using AquaBlend.Api.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace AquaBlend.Controllers;

[ApiController]
[Route("api/optimisation-results")]
public class OptimisationResultsController : ControllerBase
{
    private readonly OptimisationResultService _optimisationResultService;

    public OptimisationResultsController(
        OptimisationResultService optimisationResultService)
    {
        _optimisationResultService = optimisationResultService;
    }

    [HttpGet]
    [Authorize(Policy = AppPolicies.CanView)]
    public async Task<ActionResult<IEnumerable<OptimisationResultSummaryDto>>> GetAll()
    {
        var results = await _optimisationResultService.GetAllAsync();
        return Ok(results);
    }

    [HttpGet("scenario/{scenarioId:int}")]
    [Authorize(Policy = AppPolicies.CanView)]
    public async Task<ActionResult<IEnumerable<OptimisationResultSummaryDto>>> GetByScenario(
        int scenarioId)
    {
        var results = await _optimisationResultService.GetByScenarioAsync(scenarioId);
        return Ok(results);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = AppPolicies.CanView)]
    public async Task<ActionResult<OptimisationResultResponseDto>> GetById(int id)
    {
        var result = await _optimisationResultService.GetByIdAsync(id);

        if (result is null)
            return NotFound();

        return Ok(result);
    }
}