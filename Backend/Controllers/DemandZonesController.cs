using AquaBlend.Authorization;
using AquaBlend.DTOs.ReferenceData;
using AquaBlend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AquaBlend.Controllers;

[ApiController]
[Route("api/demand-zones")]
public class DemandZonesController : ControllerBase
{
    private readonly ReferenceDataService _referenceDataService;

    public DemandZonesController(ReferenceDataService referenceDataService)
    {
        _referenceDataService = referenceDataService;
    }

    [HttpGet]
    [Authorize(Policy = AppPolicies.CanView)]
    public async Task<ActionResult<IEnumerable<DemandZoneResponseDto>>> GetAll()
    {
        return Ok(await _referenceDataService.GetDemandZonesAsync());
    }
}
