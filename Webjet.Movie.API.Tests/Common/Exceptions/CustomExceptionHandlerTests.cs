using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text.Json;
using Webjet.Movie.API.Common.Exceptions;
using Xunit;

namespace Webjet.Movie.API.Tests.Common.Exceptions;

public class CustomExceptionHandlerTests
{
    private readonly CustomExceptionHandler _handler;
    private readonly Mock<ILogger<CustomExceptionHandler>> _mockLogger;

    public CustomExceptionHandlerTests()
    {
        _mockLogger = new Mock<ILogger<CustomExceptionHandler>>();
        _handler = new CustomExceptionHandler(_mockLogger.Object);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturnTrue_WhenExceptionIsProvided()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new Exception("Test exception");

        // Act
        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        httpContext.Response.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturnFalse_WhenExceptionIsNull()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        // Act
        var result = await _handler.TryHandleAsync(httpContext, null, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryHandleAsync_ShouldSetNotFoundStatus_WhenMovieNotFoundException()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new MovieNotFoundException("Test movie");

        // Act
        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        
        var responseBody = await GetResponseBody(httpContext);
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        errorResponse!.Error.Should().Be("Movie 'Test movie' not found");
        errorResponse.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldSetBadRequestStatus_WhenValidationException()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new ValidationException("Validation failed");

        // Act
        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        
        var responseBody = await GetResponseBody(httpContext);
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        errorResponse!.Error.Should().Be("Validation failed");
        errorResponse.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldSetServiceUnavailableStatus_WhenExternalServiceException()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new ExternalServiceException("External service error");

        // Act
        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);
        
        var responseBody = await GetResponseBody(httpContext);
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        errorResponse!.Error.Should().Be("External service error");
        errorResponse.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldSetInternalServerErrorStatus_WhenGenericException()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new InvalidOperationException("Something went wrong");

        // Act
        var result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        
        var responseBody = await GetResponseBody(httpContext);
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        errorResponse!.Error.Should().Be("Something went wrong");
        errorResponse.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldLogError_WhenExceptionOccurs()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new Exception("Test exception");

        // Act
        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static HttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }

    private static async Task<string> GetResponseBody(HttpContext httpContext)
    {
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body);
        return await reader.ReadToEndAsync();
    }

    private class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public int StatusCode { get; set; }
    }
} 