using AquaBlend.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using AquaBlend.DTOs.WaterSources;
using AquaBlend.Services;
using Microsoft.AspNetCore.Mvc;

namespace AquaBlend.Controllers;

[ApiController]
[Route("api/water-sources")]
public class WaterSourcesController : ControllerBase
{
    private readonly WaterSourceService _waterSourceService;

    public WaterSourcesController(WaterSourceService waterSourceService)
    {
        _waterSourceService = waterSourceService;
    }

    [HttpGet]
    [Authorize(Policy = AppPolicies.CanView)]
    public async Task<ActionResult<IEnumerable<WaterSourceResponseDto>>> GetAll()
    {
        return Ok(await _waterSourceService.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = AppPolicies.CanView)]
    public async Task<ActionResult<WaterSourceResponseDto>> GetById(int id)
    {
        var waterSource = await _waterSourceService.GetByIdAsync(id);
        return waterSource is null ? NotFound() : Ok(waterSource);
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.CanAdminister)]
    public async Task<ActionResult<WaterSourceResponseDto>> Create(CreateWaterSourceDto dto)
    {
        var created = await _waterSourceService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = AppPolicies.CanAdminister)]
    public async Task<IActionResult> Update(int id, UpdateWaterSourceDto dto)
    {
        return await _waterSourceService.UpdateAsync(id, dto) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AppPolicies.CanAdminister)]
    public async Task<IActionResult> Delete(int id)
    {
        return await _waterSourceService.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
