using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TurnTable.Api.Exceptions;

namespace TurnTable.Api.Middleware;

/// <summary>
/// Middleware that catches all unhandled exceptions in the HTTP pipeline and returns
/// structured JSON problem details with appropriate HTTP status codes.
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionHandlingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware delegate in the HTTP execution pipeline.</param>
    /// <param name="logger">The logger instance for recording warning and error diagnostics.</param>
    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to handle incoming HTTP requests and capture unhandled exeptions.
    /// </summary>
    /// <param name="context">The current HTTP execution context.</param>
    /// <returns>A task that represents the completion of request processing.</returns>
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

    /// <summary>
    /// Handles caught exceptions by mapping them to appropriate HTTP status codes and serializing a ProblemDetails JSON response.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="exception">The caught exception instance.</param>
    /// <returns>A task representing the response serialization process.</returns>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var title = "An unexpected error occurred.";
        var detail = exception.Message;

        switch (exception)
        {
            case BusinessValidationException:
                statusCode = HttpStatusCode.BadRequest;
                title = "Business Validation Error";
                _logger.LogWarning("Business validation failed: {ValidationMessage}", exception.Message);
                break;

            case NotFoundException:
                statusCode = HttpStatusCode.NotFound;
                title = "Resource Not Found";
                _logger.LogWarning("Resource not found: {NotFoundMessge}", exception.Message);
                break;

            default:
                _logger.LogError(exception, "Unhandled server error occurred.");
                break;
        }

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, jsonOptions));
    }
    
}