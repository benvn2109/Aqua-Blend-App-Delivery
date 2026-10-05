using AquaBlend.Authorization;
using AquaBlend.DTOs.ReferenceData;
using AquaBlend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AquaBlend.Controllers;

[ApiController]
[Route("api/quality-profiles")]
public class QualityProfilesController : ControllerBase
{
    private readonly ReferenceDataService _referenceDataService;

    public QualityProfilesController(ReferenceDataService referenceDataService)
    {
        _referenceDataService = referenceDataService;
    }

    [HttpGet]
    [Authorize(Policy = AppPolicies.CanView)]
    public async Task<ActionResult<IEnumerable<QualityProfileResponseDto>>> GetAll()
    {
        return Ok(await _referenceDataService.GetQualityProfilesAsync());
    }
}
