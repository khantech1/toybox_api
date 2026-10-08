using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace ToyBoxApi.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = ex switch
        {
            // Authentication failures are handled by the JWT middleware (401);
            // this exception means "authenticated but not allowed".
            UnauthorizedAccessException => (HttpStatusCode.Forbidden,    ex.Message),
            DbUpdateConcurrencyException => (HttpStatusCode.Conflict,
                                            "This item was changed by someone else. Please refresh and try again."),
            KeyNotFoundException        => (HttpStatusCode.NotFound,     ex.Message),
            InvalidOperationException   => (HttpStatusCode.BadRequest,   ex.Message),
            ArgumentException           => (HttpStatusCode.BadRequest,   ex.Message),
            _                           => (HttpStatusCode.InternalServerError,
                                            "An unexpected error occurred. Please try again."),
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            message,
            statusCode = (int)statusCode,
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
