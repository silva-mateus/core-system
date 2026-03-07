using System.Text.Json;
using Core.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.Middleware;

/// <summary>
/// Configuration options for the global exception handler middleware.
/// Allows consuming apps to register custom exception-to-ProblemDetails mappings.
/// </summary>
public class CoreExceptionHandlerOptions
{
    internal Dictionary<Type, Func<Exception, ProblemDetails>> CustomMappings { get; } = new();

    /// <summary>
    /// Registers a custom mapping from an exception type to a ProblemDetails response.
    /// </summary>
    public void MapException<TException>(Func<TException, ProblemDetails> mapper) where TException : Exception
    {
        CustomMappings[typeof(TException)] = ex => mapper((TException)ex);
    }
}

/// <summary>
/// Catches unhandled exceptions and maps them to RFC 7807 ProblemDetails responses.
/// Provides built-in mappings for Core exceptions and allows apps to extend with custom mappings.
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly CoreExceptionHandlerOptions _options;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        CoreExceptionHandlerOptions options)
    {
        _next = next;
        _logger = logger;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        ProblemDetails problemDetails;

        if (_options.CustomMappings.TryGetValue(exception.GetType(), out var customMapper))
        {
            problemDetails = customMapper(exception);
        }
        else
        {
            problemDetails = exception switch
            {
                NotFoundException notFoundEx => new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Recurso não encontrado",
                    Detail = notFoundEx.Message,
                    Extensions = { ["errorCode"] = notFoundEx.ErrorCode }
                },
                ForbiddenException forbiddenEx => new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Acesso negado",
                    Detail = forbiddenEx.Message,
                    Extensions = { ["errorCode"] = forbiddenEx.ErrorCode }
                },
                ConflictException conflictEx => new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Conflito",
                    Detail = conflictEx.Message,
                    Extensions = { ["errorCode"] = conflictEx.ErrorCode }
                },
                BusinessRuleException businessEx => new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Erro de negócio",
                    Detail = businessEx.Message,
                    Extensions = { ["errorCode"] = businessEx.ErrorCode }
                },
                _ => new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Erro interno do servidor",
                    Detail = "Ocorreu um erro inesperado. Tente novamente mais tarde.",
                    Extensions = { ["traceId"] = context.TraceIdentifier }
                }
            };
        }

        if (problemDetails.Status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception (TraceId: {TraceId}): {Message}",
                context.TraceIdentifier, exception.Message);
        }
        else
        {
            _logger.LogWarning("Handled exception: {ExceptionType} - {Message}",
                exception.GetType().Name, exception.Message);
        }

        context.Response.StatusCode = problemDetails.Status ?? 500;
        context.Response.ContentType = "application/problem+json";

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
    }
}
