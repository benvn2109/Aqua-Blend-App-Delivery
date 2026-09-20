using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using AquaBlend.DTOs.Common;

namespace AquaBlend.Api.Middleware;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await WriteErrorResponseAsync(
                context,
                StatusCodes.Status400BadRequest,
                "Validation failed",
                new[]
                {
                    new ApiErrorDetailDto
                    {
                        Message = ex.Message
                    }
                });
        }
        catch (KeyNotFoundException ex)
        {
            await WriteErrorResponseAsync(
                context,
                StatusCodes.Status404NotFound,
                "Resource not found",
                new[]
                {
                    new ApiErrorDetailDto
                    {
                        Message = ex.Message
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await WriteErrorResponseAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Internal server error",
                Array.Empty<ApiErrorDetailDto>());
        }
    }

    private static async Task WriteErrorResponseAsync(
        HttpContext context,
        int statusCode,
        string error,
        IEnumerable<ApiErrorDetailDto> details)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new ApiErrorResponseDto
        {
            Status = statusCode,
            Error = error,
            Details = details.ToArray(),
            Timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response));
    }
}