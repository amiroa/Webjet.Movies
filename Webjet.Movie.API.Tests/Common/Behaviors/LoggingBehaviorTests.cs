using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Webjet.Movie.API.Common.Behaviors;
using Webjet.Movie.API.Features.Movies;
using Xunit;
using MediatR;

namespace Webjet.Movie.API.Tests.Common.Behaviors;

public class LoggingBehaviorTests
{
    private readonly LoggingBehavior<GetMoviesRequest, GetMoviesResponse> _behavior;
    private readonly Mock<ILogger<LoggingBehavior<GetMoviesRequest, GetMoviesResponse>>> _mockLogger;

    public LoggingBehaviorTests()
    {
        _mockLogger = new Mock<ILogger<LoggingBehavior<GetMoviesRequest, GetMoviesResponse>>>();
        _behavior = new LoggingBehavior<GetMoviesRequest, GetMoviesResponse>(_mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldLogStartAndEnd_WhenRequestSucceeds()
    {
        // Arrange
        var request = new GetMoviesRequest(1, 10, "", "", false);
        var expectedResponse = new GetMoviesResponse(new List<MovieSummary>(), new PaginationInfo(1, 10, 0, 0, false, false));

        RequestHandlerDelegate<GetMoviesResponse> next = () => Task.FromResult(expectedResponse);

        // Act
        var result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[START]")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[END]")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldLogPerformance_WhenRequestTakesTime()
    {
        // Arrange
        var request = new GetMoviesRequest(1, 10, "", "", false);
        var expectedResponse = new GetMoviesResponse(new List<MovieSummary>(), new PaginationInfo(1, 10, 0, 0, false, false));

        RequestHandlerDelegate<GetMoviesResponse> next = async () =>
        {
            await Task.Delay(3500); // Simulate work that takes more than 3 seconds
            return expectedResponse;
        };

        // Act
        var result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);

        // Debug print
        foreach (var invocation in _mockLogger.Invocations)
        {
            Console.WriteLine($"Method: {invocation.Method.Name}");
            for (int i = 0; i < invocation.Arguments.Count; i++)
            {
                var arg = invocation.Arguments[i];
                Console.WriteLine($"  Arg[{i}]: {arg?.GetType().Name ?? "null"} = {arg}");
            }
        }

        var found = _mockLogger.Invocations.Any(invocation =>
            invocation.Method.Name == nameof(ILogger.Log) &&
            invocation.Arguments.Count > 2 &&
            invocation.Arguments[0] is LogLevel &&
            (LogLevel)invocation.Arguments[0] == LogLevel.Warning &&
            invocation.Arguments[2].ToString()!.Contains("[PERFORMANCE]"));
        found.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldLogError_WhenExceptionOccurs()
    {
        // Arrange
        var request = new GetMoviesRequest(1, 10, "", "", false);
        var expectedException = new InvalidOperationException("Test exception");

        RequestHandlerDelegate<GetMoviesResponse> next = () => throw expectedException;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _behavior.Handle(request, next, CancellationToken.None));

        exception.Should().Be(expectedException);

        // Debug print
        foreach (var invocation in _mockLogger.Invocations)
        {
            Console.WriteLine($"Method: {invocation.Method.Name}");
            for (int i = 0; i < invocation.Arguments.Count; i++)
            {
                var arg = invocation.Arguments[i];
                Console.WriteLine($"  Arg[{i}]: {arg?.GetType().Name ?? "null"} = {arg}");
            }
        }

        var found = _mockLogger.Invocations.Any(invocation =>
            invocation.Method.Name == nameof(ILogger.Log) &&
            invocation.Arguments.Count > 2 &&
            invocation.Arguments[0] is LogLevel &&
            (LogLevel)invocation.Arguments[0] == LogLevel.Error &&
            invocation.Arguments[3] == expectedException);
        found.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldLogRequestData_WhenRequestIsProcessed()
    {
        // Arrange
        var request = new GetMoviesRequest(2, 5, "Star Wars", "title", true);
        var expectedResponse = new GetMoviesResponse(new List<MovieSummary>(), new PaginationInfo(1, 10, 0, 0, false, false));

        RequestHandlerDelegate<GetMoviesResponse> next = () => Task.FromResult(expectedResponse);

        // Act
        await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Page = 2") && 
                                               v.ToString()!.Contains("PageSize = 5") &&
                                               v.ToString()!.Contains("SearchTerm = Star Wars") &&
                                               v.ToString()!.Contains("SortBy = title") &&
                                               v.ToString()!.Contains("SortDescending = True")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
} 