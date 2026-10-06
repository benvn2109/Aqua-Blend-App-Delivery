using AquaBlend.Authorization;
using AquaBlend.DTOs.OptimisationResults;
using AquaBlend.DTOs.Runs;
using AquaBlend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AquaBlend.Controllers;

[ApiController]
[Route("api/runs")]
public class RunsController : ControllerBase
{
    private readonly RunService _runService;

    public RunsController(RunService runService)
    {
        _runService = runService;
    }

    [HttpGet("{runId:int}")]
    [Authorize(Policy = AppPolicies.CanView)]
    public async Task<ActionResult<RunResponseDto>> GetById(int runId)
    {
        var run = await _runService.GetByIdAsync(runId);

        if (run is null)
            return NotFound();

        return Ok(run);
    }

    [HttpGet("{runId:int}/results")]
    [Authorize(Policy = AppPolicies.CanView)]
    public async Task<ActionResult<OptimisationResultResponseDto>> GetResult(int runId)
    {
        var run = await _runService.GetByIdAsync(runId);

        if (run is null)
            return NotFound();

        var result = await _runService.GetResultAsync(runId);

        if (result is null)
            return NotFound();

        return Ok(result);
    }
}