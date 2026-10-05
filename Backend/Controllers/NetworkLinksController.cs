using AquaBlend.Authorization;
using AquaBlend.DTOs.ReferenceData;
using AquaBlend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AquaBlend.Controllers;

[ApiController]
[Route("api/network-links")]
public class NetworkLinksController : ControllerBase
{
    private readonly ReferenceDataService _referenceDataService;

    public NetworkLinksController(ReferenceDataService referenceDataService)
    {
        _referenceDataService = referenceDataService;
    }

    [HttpGet]
    [Authorize(Policy = AppPolicies.CanView)]
    public async Task<ActionResult<NetworkLinksResponseDto>> GetAll()
    {
        return Ok(await _referenceDataService.GetNetworkLinksAsync());
    }
}
