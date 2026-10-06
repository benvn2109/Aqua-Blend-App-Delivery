using AquaBlend.Authorization;
using Microsoft.AspNetCore.Authorization;
using AquaBlend.DTOs.Scenarios;
using AquaBlend.Services;
using Microsoft.AspNetCore.Mvc;
using AquaBlend.DTOs.Runs;

namespace AquaBlend.Controllers
{
    [ApiController]
    [Route("api/scenarios")]
    public class ScenariosController : ControllerBase
    {
        private readonly ScenarioService _scenarioService;
        private readonly ScenarioValidationService _scenarioValidationService;
        private readonly RunService _runService;

        public ScenariosController(
            ScenarioService scenarioService,
            ScenarioValidationService scenarioValidationService,
            RunService runService)
        {
            _scenarioService = scenarioService;
            _scenarioValidationService = scenarioValidationService;
            _runService = runService;
        }

        [HttpGet]
        [Authorize(Policy = AppPolicies.CanView)]
        public async Task<ActionResult<IEnumerable<ScenarioResponseDto>>> GetAll()
        {
            var scenarios = await _scenarioService.GetAllAsync();
            return Ok(scenarios);
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = AppPolicies.CanView)]
        public async Task<ActionResult<ScenarioResponseDto>> GetById(int id)
        {
            var scenario = await _scenarioService.GetByIdAsync(id);

            if (scenario == null)
                return NotFound();

            return Ok(scenario);
        }

        [HttpPost]
        [Authorize(Policy = AppPolicies.CanAnalyse)]
        public async Task<ActionResult<ScenarioResponseDto>> Create(CreateScenarioDto dto)
        {
            var created = await _scenarioService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                created);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = AppPolicies.CanAnalyse)]
        public async Task<IActionResult> Update(int id, UpdateScenarioDto dto)
        {
            var updated = await _scenarioService.UpdateAsync(id, dto);

            if (!updated)
                return NotFound();

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = AppPolicies.CanAdminister)]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _scenarioService.DeleteAsync(id);

            if (!deleted)
                return NotFound();

            return NoContent();
        }

        [HttpPost("{id:int}/validate")]
        [Authorize(Policy = AppPolicies.CanAnalyse)]
        public async Task<ActionResult<ScenarioResponseDto>> Validate(int id)
        {
            var result = await _scenarioValidationService.ValidateAsync(id);

            if (!result.Found)
                return NotFound();

            var scenario = await _scenarioService.GetByIdAsync(id);

            if (scenario is null)
                return NotFound();

            return Ok(scenario);
        }

        [HttpPost("{id:int}/runs")]
        [Authorize(Policy = AppPolicies.CanAnalyse)]
        public async Task<ActionResult<RunResponseDto>> CreateRun(int id)
        {
            try
            {
                var run = await _runService.CreateAsync(id);

                if (run is null)
                    return NotFound();

                return Created(
                    $"/api/runs/{run.Id}",
                    run);
            }
            catch (ScenarioNotReadyException ex)
            {
                return Conflict(new
                {
                    code = "scenario_not_ready",
                    message = ex.Message
                });
            }
        }

        [HttpGet("{id:int}/runs")]
        [Authorize(Policy = AppPolicies.CanView)]
        public async Task<ActionResult<IEnumerable<RunResponseDto>>> GetRuns(int id)
        {
            var scenario = await _scenarioService.GetByIdAsync(id);

            if (scenario is null)
                return NotFound();

            var runs = await _runService.GetByScenarioAsync(id);

            return Ok(runs);
        }
    }
}