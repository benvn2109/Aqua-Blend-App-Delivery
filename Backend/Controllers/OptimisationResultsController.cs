using AquaBlend.DTOs.OptimisationResults;
using AquaBlend.Services;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<ActionResult<IEnumerable<OptimisationResultSummaryDto>>> GetAll()
    {
        var results = await _optimisationResultService.GetAllAsync();
        return Ok(results);
    }

    [HttpGet("scenario/{scenarioId:int}")]
    public async Task<ActionResult<IEnumerable<OptimisationResultSummaryDto>>> GetByScenario(
        int scenarioId)
    {
        var results = await _optimisationResultService.GetByScenarioAsync(scenarioId);
        return Ok(results);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OptimisationResultResponseDto>> GetById(int id)
    {
        var result = await _optimisationResultService.GetByIdAsync(id);

        if (result is null)
            return NotFound();

        return Ok(result);
    }
}