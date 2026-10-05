using AquaBlend.Authorization;
using AquaBlend.DTOs.ReferenceData;
using AquaBlend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AquaBlend.Controllers;

[ApiController]
[Route("api/sources")]
public class SourcesController : ControllerBase
{
    private readonly ReferenceDataService _referenceDataService;

    public SourcesController(ReferenceDataService referenceDataService)
    {
        _referenceDataService = referenceDataService;
    }

    [HttpGet]
    [Authorize(Policy = AppPolicies.CanView)]
    public async Task<ActionResult<IEnumerable<SourceResponseDto>>> GetAll()
    {
        return Ok(await _referenceDataService.GetSourcesAsync());
    }
}
