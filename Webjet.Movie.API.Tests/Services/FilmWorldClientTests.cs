using FluentAssertions;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;
using Webjet.Movie.API.Services;
using Xunit;

namespace Webjet.Movie.API.Tests.Services;

public class FilmWorldClientTests : TestBase
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly FilmWorldClient _client;

    public FilmWorldClientTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://test-filmworld.com/")
        };

        _client = new FilmWorldClient(
            _httpClient,
            GetLogger<FilmWorldClient>(),
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
            new("fw1", "Star Wars: Episode V", "movie", "1980", "poster1.jpg"),
            new("fw2", "The Godfather", "movie", "1972", "poster2.jpg")
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
        result.First().Title.Should().Be("Star Wars: Episode V");
        result.First().ID.Should().Be("fw1");
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
            new("fw1", "Star Wars: Episode V", "movie", "1980", "poster1.jpg")
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
            "fw1", "Star Wars: Episode V - The Empire Strikes Back", "movie", "1980",
            "PG", "1980-05-21", "124 min", "Action, Adventure, Fantasy",
            "Irvin Kershner", "Leigh Brackett, Lawrence Kasdan", "Mark Hamill, Harrison Ford, Carrie Fisher",
            "Luke Skywalker joins forces with a Jedi Knight...", "English", "United States",
            "Won 1 Oscar", "poster1.jpg", "82", "8.7", "1,345,678", "20.00"
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
        var result = await _client.GetMovieDetailsAsync("fw1", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Star Wars: Episode V - The Empire Strikes Back");
        result.Director.Should().Be("Irvin Kershner");
        result.Price.Should().Be("20.00");
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
        var result = await _client.GetMovieDetailsAsync("fw1", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ProviderName_ShouldReturnFilmWorld()
    {
        // Act & Assert
        _client.ProviderName.Should().Be("FilmWorld");
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
        capturedRequest.Headers.GetValues("x-access-token").Should().Contain("test-film-key");
    }
} 