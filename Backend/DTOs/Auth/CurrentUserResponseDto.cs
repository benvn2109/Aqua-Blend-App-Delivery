namespace AquaBlend.Api.DTOs.Auth;

public sealed record CurrentUserResponseDto(
    string? UserId,
    string? Username,
    IReadOnlyCollection<string> Roles);