using System.Security.Claims;
using AquaBlend.Api.Authorization;
using AquaBlend.Api.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AquaBlend.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    [Authorize(Policy = AppPolicies.CanView)]
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResponseDto>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<CurrentUserResponseDto> GetCurrentUser()
    {
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        var username =
            User.Identity?.Name
            ?? User.FindFirstValue("name");

        var roles = User.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct()
            .ToArray();

        return Ok(new CurrentUserResponseDto(
            userId,
            username,
            roles));
    }
}