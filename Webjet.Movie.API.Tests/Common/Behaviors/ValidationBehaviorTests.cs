using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using Webjet.Movie.API.Common.Behaviors;
using Webjet.Movie.API.Features.Movies;
using Xunit;

namespace Webjet.Movie.API.Tests.Common.Behaviors;

public class ValidationBehaviorTests
{
    private readonly ValidationBehavior<GetMoviesRequest, GetMoviesResponse> _behavior;
    private readonly Mock<IValidator<GetMoviesRequest>> _mockValidator;

    public ValidationBehaviorTests()
    {
        _mockValidator = new Mock<IValidator<GetMoviesRequest>>();
        _behavior = new ValidationBehavior<GetMoviesRequest, GetMoviesResponse>(new[] { _mockValidator.Object });
    }

    [Fact]
    public async Task Handle_ShouldCallNext_WhenNoValidationErrors()
    {
        // Arrange
        var request = new GetMoviesRequest(1, 10, "", "", false);
        var expectedResponse = new GetMoviesResponse(new List<MovieSummary>(), new PaginationInfo(1, 10, 0, 0, false, false));
        var nextCalled = false;

        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<GetMoviesRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        RequestHandlerDelegate<GetMoviesResponse> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(expectedResponse);
        };

        // Act
        var result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenValidationFails()
    {
        // Arrange
        var request = new GetMoviesRequest(-1, -10, "", "", false); // Invalid values
        var nextCalled = false;

        var validationFailures = new List<ValidationFailure>
        {
            new("Page", "Page must be greater than 0"),
            new("PageSize", "PageSize must be greater than 0")
        };

        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<GetMoviesRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        RequestHandlerDelegate<GetMoviesResponse> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new GetMoviesResponse(new List<MovieSummary>(), new PaginationInfo(1, 10, 0, 0, false, false)));
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _behavior.Handle(request, next, CancellationToken.None));

        exception.Should().NotBeNull();
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldHandleMultipleValidators()
    {
        // Arrange
        var request = new GetMoviesRequest(1, 10, "", "", false);
        var mockValidator1 = new Mock<IValidator<GetMoviesRequest>>();
        var mockValidator2 = new Mock<IValidator<GetMoviesRequest>>();

        mockValidator1.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<GetMoviesRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        mockValidator2.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<GetMoviesRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var behavior = new ValidationBehavior<GetMoviesRequest, GetMoviesResponse>(new[] { mockValidator1.Object, mockValidator2.Object });

        var expectedResponse = new GetMoviesResponse(new List<MovieSummary>(), new PaginationInfo(1, 10, 0, 0, false, false));

        RequestHandlerDelegate<GetMoviesResponse> next = () => Task.FromResult(expectedResponse);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        mockValidator1.Verify(v => v.ValidateAsync(It.IsAny<ValidationContext<GetMoviesRequest>>(), It.IsAny<CancellationToken>()), Times.Once);
        mockValidator2.Verify(v => v.ValidateAsync(It.IsAny<ValidationContext<GetMoviesRequest>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
} 