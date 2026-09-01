using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AquaBlend.Tests;

internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
{
    if (Request.Headers.TryGetValue("X-Test-Anonymous", out var anonymous) &&
        string.Equals(
            anonymous.ToString(),
            "true",
            StringComparison.OrdinalIgnoreCase))
    {
        return Task.FromResult(AuthenticateResult.NoResult());
    }

    var role =
        Request.Headers.TryGetValue("X-Test-Role", out var requestedRole) &&
        !string.IsNullOrWhiteSpace(requestedRole.ToString())
            ? requestedRole.ToString()
            : "Admin";

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, "integration-test-user"),
        new Claim(ClaimTypes.Name, "Integration Test User"),
        new Claim(ClaimTypes.Role, role)
    };

    var identity = new ClaimsIdentity(claims, SchemeName);
    var principal = new ClaimsPrincipal(identity);
    var ticket = new AuthenticationTicket(principal, SchemeName);

    return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}