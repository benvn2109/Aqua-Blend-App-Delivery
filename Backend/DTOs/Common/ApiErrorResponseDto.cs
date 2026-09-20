namespace AquaBlend.DTOs.Common;

public sealed class ApiErrorResponseDto
{
    public int Status { get; init; }
    public string Error { get; init; } = string.Empty;
    public IReadOnlyList<ApiErrorDetailDto> Details { get; init; } = Array.Empty<ApiErrorDetailDto>();
    public DateTime Timestamp { get; init; }
}

public sealed class ApiErrorDetailDto
{
    public string? Field { get; init; }
    public string Message { get; init; } = string.Empty;
}