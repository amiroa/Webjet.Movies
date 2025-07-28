using Microsoft.AspNetCore.Diagnostics;
using System.Net;

namespace Webjet.Movie.API.Common.Exceptions;

public class CustomExceptionHandler : IExceptionHandler
{
    private readonly ILogger<CustomExceptionHandler> _logger;

    public CustomExceptionHandler(ILogger<CustomExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception == null)
        {
            return false;
        }

        _logger.LogError(exception, "An unhandled exception occurred");

        var statusCode = exception switch
        {
            MovieNotFoundException => HttpStatusCode.NotFound,
            ValidationException => HttpStatusCode.BadRequest,
            ExternalServiceException => HttpStatusCode.ServiceUnavailable,
            _ => HttpStatusCode.InternalServerError
        };

        var response = new
        {
            error = exception.Message,
            statusCode = (int)statusCode
        };

        httpContext.Response.StatusCode = (int)statusCode;
        httpContext.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(response);
        await httpContext.Response.WriteAsync(json, cancellationToken);

        return true;
    }
} 