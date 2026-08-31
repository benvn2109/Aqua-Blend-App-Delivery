using AquaBlend.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using AquaBlend.DTOs.Scenarios;
using AquaBlend.Services;
using Microsoft.AspNetCore.Mvc;

namespace AquaBlend.Controllers
{
    [ApiController]
    [Route("api/scenarios")]
    public class ScenariosController : ControllerBase
    {
        private readonly ScenarioService _scenarioService;

        public ScenariosController(ScenarioService scenarioService)
        {
            _scenarioService = scenarioService;
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
    }
}