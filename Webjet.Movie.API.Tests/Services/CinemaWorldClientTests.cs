using FluentAssertions;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;
using Webjet.Movie.API.Services;
using Xunit;

namespace Webjet.Movie.API.Tests.Services;

public class CinemaWorldClientTests : TestBase
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly CinemaWorldClient _client;

    public CinemaWorldClientTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://test-cinemaworld.com/")
        };

        _client = new CinemaWorldClient(
            _httpClient,
            GetLogger<CinemaWorldClient>(),
            GetMemoryCache(),
            GetProvidersConfig(),
            GetCacheSettings()
        );
    }

    [Fact]
    public async Task GetMoviesAsync_ShouldReturnMovies_WhenApiReturnsSuccess()
    {
        // Arrange
        var moviesResponse = new ProviderMoviesResponse(new List<ProviderMovieDto>
        {
            new("cw1", "Star Wars: Episode IV", "movie", "1977", "poster1.jpg"),
            new("cw2", "The Matrix", "movie", "1999", "poster2.jpg")
        });

        var responseContent = JsonSerializer.Serialize(moviesResponse);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _client.GetMoviesAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Title.Should().Be("Star Wars: Episode IV");
        result.First().ID.Should().Be("cw1");
    }

    [Fact]
    public async Task GetMoviesAsync_ShouldReturnEmptyList_WhenApiReturnsError()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _client.GetMoviesAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMoviesAsync_ShouldReturnCachedMovies_WhenCalledMultipleTimes()
    {
        // Arrange
        var moviesResponse = new ProviderMoviesResponse(new List<ProviderMovieDto>
        {
            new("cw1", "Star Wars: Episode IV", "movie", "1977", "poster1.jpg")
        });

        var responseContent = JsonSerializer.Serialize(moviesResponse);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result1 = await _client.GetMoviesAsync(CancellationToken.None);
        var result2 = await _client.GetMoviesAsync(CancellationToken.None);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.Should().BeEquivalentTo(result2);

        // Verify HTTP call was made only once (second call should use cache)
        _mockHttpMessageHandler
            .Protected()
            .Verify<Task<HttpResponseMessage>>(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetMovieDetailsAsync_ShouldReturnMovieDetails_WhenApiReturnsSuccess()
    {
        // Arrange
        var movieDetail = new ProviderMovieDetailDto(
            "cw1", "Star Wars: Episode IV - A New Hope", "movie", "1977",
            "PG", "1977-05-25", "121 min", "Action, Adventure, Fantasy",
            "George Lucas", "George Lucas", "Mark Hamill, Harrison Ford, Carrie Fisher",
            "Luke Skywalker joins forces with a Jedi Knight...", "English", "United States",
            "Won 6 Oscars", "poster1.jpg", "90", "8.6", "1,234,567", "25.00"
        );

        var responseContent = JsonSerializer.Serialize(movieDetail);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _client.GetMovieDetailsAsync("cw1", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Star Wars: Episode IV - A New Hope");
        result.Director.Should().Be("George Lucas");
        result.Price.Should().Be("25.00");
    }

    [Fact]
    public async Task GetMovieDetailsAsync_ShouldReturnNull_WhenApiReturnsError()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _client.GetMovieDetailsAsync("cw1", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ProviderName_ShouldReturnCinemaWorld()
    {
        // Act & Assert
        _client.ProviderName.Should().Be("CinemaWorld");
    }

    [Fact]
    public async Task GetMoviesAsync_ShouldIncludeAccessToken_WhenMakingRequest()
    {
        // Arrange
        var moviesResponse = new ProviderMoviesResponse(new List<ProviderMovieDto>());

        var responseContent = JsonSerializer.Serialize(moviesResponse);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        HttpRequestMessage? capturedRequest = null;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, token) => capturedRequest = request)
            .ReturnsAsync(response);

        // Act
        await _client.GetMoviesAsync(CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Should().Contain(h => h.Key == "x-access-token");
        capturedRequest.Headers.GetValues("x-access-token").Should().Contain("test-cinema-key");
    }
} 