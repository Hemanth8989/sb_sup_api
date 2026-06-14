using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StoneBridge.Application.Common.Exceptions;
using ValidationException = StoneBridge.Application.Common.Exceptions.ValidationException;

namespace StoneBridge.Api.Middleware;

/// <summary>
/// Global exception handler — converts domain exceptions to RFC 7807 ProblemDetails responses.
/// Must be the FIRST middleware in the pipeline so it catches exceptions from all downstream.
/// In production environments: stack traces are hidden from the response (logged only).
/// In development: full exception detail included in the response for debugging.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate              _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment             _environment;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented        = false,
    };

    public ExceptionHandlingMiddleware(
        RequestDelegate                       next,
        ILogger<ExceptionHandlingMiddleware>  logger,
        IHostEnvironment                      environment)
    {
        _next        = next;
        _logger      = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (statusCode, problem) = MapException(context, exception);

        if (statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "Unhandled exception: {Method} {Path} | CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);
        }
        else
        {
            _logger.LogWarning(
                "{ExceptionType}: {Message} | {Method} {Path}",
                exception.GetType().Name,
                exception.Message,
                context.Request.Method,
                context.Request.Path);
        }

        context.Response.StatusCode  = statusCode;
        context.Response.ContentType = "application/problem+json";

        var json = JsonSerializer.Serialize(problem, problem.GetType(), JsonOptions);
        await context.Response.WriteAsync(json);
    }

    private (int statusCode, ProblemDetails problem) MapException(HttpContext context, Exception ex) =>
        ex switch
        {
            ValidationException ve => (
                StatusCodes.Status422UnprocessableEntity,
                new ValidationProblemDetails(
                    ve.Errors.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value,
                        StringComparer.OrdinalIgnoreCase))
                {
                    Title    = "One or more validation errors occurred.",
                    Status   = StatusCodes.Status422UnprocessableEntity,
                    Type     = "https://tools.ietf.org/html/rfc4918#section-11.2",
                    Instance = context.Request.Path,
                }),

            NotFoundException nfe => (
                StatusCodes.Status404NotFound,
                new ProblemDetails
                {
                    Title    = "Resource not found.",
                    Detail   = nfe.Message,
                    Status   = StatusCodes.Status404NotFound,
                    Type     = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Instance = context.Request.Path,
                }),

            ForbiddenException fe => (
                StatusCodes.Status403Forbidden,
                new ProblemDetails
                {
                    Title    = "Access forbidden.",
                    Detail   = fe.Message,
                    Status   = StatusCodes.Status403Forbidden,
                    Type     = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    Instance = context.Request.Path,
                }),

            BusinessRuleException bre => (
                StatusCodes.Status400BadRequest,
                new ProblemDetails
                {
                    Title    = "Business rule violation.",
                    Detail   = bre.Message,
                    Status   = StatusCodes.Status400BadRequest,
                    Type     = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = context.Request.Path,
                    Extensions = { ["rule"] = bre.RuleName },
                }),

            UnauthorizedAccessException uae => (
                StatusCodes.Status401Unauthorized,
                new ProblemDetails
                {
                    Title    = "Unauthorised.",
                    Detail   = _environment.IsDevelopment() ? uae.Message : "Authentication required.",
                    Status   = StatusCodes.Status401Unauthorized,
                    Type     = "https://tools.ietf.org/html/rfc7235#section-3.1",
                    Instance = context.Request.Path,
                }),

            _ => (
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title    = "An unexpected error occurred.",
                    Detail   = _environment.IsDevelopment()
                                   ? ex.ToString()
                                   : "An internal error occurred. Please try again later.",
                    Status   = StatusCodes.Status500InternalServerError,
                    Type     = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Instance = context.Request.Path,
                })
        };
}