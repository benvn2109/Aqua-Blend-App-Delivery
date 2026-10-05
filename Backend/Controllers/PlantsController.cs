using AquaBlend.Authorization;
using AquaBlend.DTOs.ReferenceData;
using AquaBlend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AquaBlend.Controllers;

[ApiController]
[Route("api/plants")]
public class PlantsController : ControllerBase
{
    private readonly ReferenceDataService _referenceDataService;

    public PlantsController(ReferenceDataService referenceDataService)
    {
        _referenceDataService = referenceDataService;
    }

    [HttpGet]
    [Authorize(Policy = AppPolicies.CanView)]
    public async Task<ActionResult<IEnumerable<PlantResponseDto>>> GetAll()
    {
        return Ok(await _referenceDataService.GetPlantsAsync());
    }
}
