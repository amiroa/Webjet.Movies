using FluentAssertions;
using Moq;
using Webjet.Movie.API.Common.Exceptions;
using Webjet.Movie.API.Features.Movies;
using Webjet.Movie.API.Services;
using Xunit;

namespace Webjet.Movie.API.Tests.Features.Movies;

public class GetMovieDetailsHandlerTests : TestBase
{
    private GetMovieDetailsHandler _handler = null!;
    private Mock<CinemaWorldClient> _mockCinemaWorldClient = null!;
    private Mock<FilmWorldClient> _mockFilmWorldClient = null!;

    public GetMovieDetailsHandlerTests()
    {
        SetupHandler();
    }

    private void SetupHandler()
    {
        _mockCinemaWorldClient = new Mock<CinemaWorldClient>(
            MockBehavior.Loose,
            new HttpClient(),
            GetLogger<CinemaWorldClient>(),
            GetMemoryCache(),
            GetProvidersConfig(),
            GetCacheSettings()
        );

        _mockFilmWorldClient = new Mock<FilmWorldClient>(
            MockBehavior.Loose,
            new HttpClient(),
            GetLogger<FilmWorldClient>(),
            GetMemoryCache(),
            GetProvidersConfig(),
            GetCacheSettings()
        );

        var providers = new List<IProviderClient> { _mockCinemaWorldClient.Object, _mockFilmWorldClient.Object };
        _handler = new GetMovieDetailsHandler(providers, GetLogger<GetMovieDetailsHandler>());
    }

    [Fact]
    public async Task Handle_ShouldReturnMovieDetails_WhenMovieFoundInBothProviders()
    {
        // Arrange
        var cinemaMovie = new ProviderMovieDetailDto("cw1", "Star Wars: Episode IV", "movie", "1977", "PG", "1977-05-25", "121 min", "Action, Adventure, Fantasy", "George Lucas", "George Lucas", "Mark Hamill, Harrison Ford, Carrie Fisher", "A long time ago...", "English", "United States", "Won 6 Oscars", "poster1.jpg", "90", "8.6", "1,234,567", "25.00");
        var filmMovie = new ProviderMovieDetailDto("fw1", "Star Wars: Episode IV", "movie", "1977", "PG", "1977-05-25", "121 min", "Action, Adventure, Fantasy", "George Lucas", "George Lucas", "Mark Hamill, Harrison Ford, Carrie Fisher", "A long time ago...", "English", "United States", "Won 6 Oscars", "poster1.jpg", "90", "8.6", "1,234,567", "20.00");

        _mockCinemaWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderMovieDto> { new("cw1", "Star Wars: Episode IV", "movie", "1977", "poster1.jpg") });

        _mockFilmWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderMovieDto> { new("fw1", "Star Wars: Episode IV", "movie", "1977", "poster1.jpg") });

        _mockCinemaWorldClient.Setup(x => x.GetMovieDetailsAsync("cw1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cinemaMovie);

        _mockFilmWorldClient.Setup(x => x.GetMovieDetailsAsync("fw1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(filmMovie);

        var request = new GetMovieDetailsRequest("Star Wars: Episode IV");

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MovieDetails.Title.Should().Be("Star Wars: Episode IV");
        result.MovieDetails.Year.Should().Be(1977);
        result.MovieDetails.Type.Should().Be("movie");
        result.MovieDetails.PosterUrl.Should().Be("poster1.jpg");
        result.MovieDetails.CheapestPrice.Should().Be(20.00m);
        result.MovieDetails.CinemaWorldPrice.Should().Be(25.00m);
        result.MovieDetails.FilmWorldPrice.Should().Be(20.00m);
    }

    [Fact]
    public async Task Handle_ShouldReturnMovieDetails_WhenMovieFoundInOneProvider()
    {
        // Arrange
        var cinemaMovie = new ProviderMovieDetailDto("cw1", "Star Wars: Episode IV", "movie", "1977", "PG", "1977-05-25", "121 min", "Action, Adventure, Fantasy", "George Lucas", "George Lucas", "Mark Hamill, Harrison Ford, Carrie Fisher", "A long time ago...", "English", "United States", "Won 6 Oscars", "poster1.jpg", "90", "8.6", "1,234,567", "25.00");

        _mockCinemaWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderMovieDto> { new("cw1", "Star Wars: Episode IV", "movie", "1977", "poster1.jpg") });

        _mockFilmWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderMovieDto>());

        _mockCinemaWorldClient.Setup(x => x.GetMovieDetailsAsync("cw1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cinemaMovie);

        var request = new GetMovieDetailsRequest("Star Wars: Episode IV");

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MovieDetails.Title.Should().Be("Star Wars: Episode IV");
        result.MovieDetails.Year.Should().Be(1977);
        result.MovieDetails.Type.Should().Be("movie");
        result.MovieDetails.PosterUrl.Should().Be("poster1.jpg");
        result.MovieDetails.CinemaWorldPrice.Should().Be(25.00m);
        result.MovieDetails.FilmWorldPrice.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldThrowMovieNotFoundException_WhenMovieNotFoundInAnyProvider()
    {
        // Arrange
        _mockCinemaWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderMovieDto>());

        _mockFilmWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderMovieDto>());

        var request = new GetMovieDetailsRequest("Non-existent Movie");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<MovieNotFoundException>(() =>
            _handler.Handle(request, CancellationToken.None));

        exception.Message.Should().Be("Movie 'Non-existent Movie' not found");
    }

    [Fact]
    public async Task Handle_ShouldThrowExternalServiceException_WhenAllProvidersFailToGetDetails()
    {
        // Arrange
        _mockCinemaWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderMovieDto> { new("cw1", "Star Wars: Episode IV", "movie", "1977", "poster1.jpg") });

        _mockFilmWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderMovieDto> { new("fw1", "Star Wars: Episode IV", "movie", "1977", "poster1.jpg") });

        _mockCinemaWorldClient.Setup(x => x.GetMovieDetailsAsync("cw1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProviderMovieDetailDto?)null);

        _mockFilmWorldClient.Setup(x => x.GetMovieDetailsAsync("fw1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProviderMovieDetailDto?)null);

        var request = new GetMovieDetailsRequest("Star Wars: Episode IV");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ExternalServiceException>(() =>
            _handler.Handle(request, CancellationToken.None));

        exception.Message.Should().Be("Failed to retrieve movie details from all providers");
    }

    [Fact]
    public async Task Handle_ShouldHandleCaseInsensitiveTitleMatching()
    {
        // Arrange
        var cinemaMovie = new ProviderMovieDetailDto("cw1", "Star Wars: Episode IV", "movie", "1977", "PG", "1977-05-25", "121 min", "Action, Adventure, Fantasy", "George Lucas", "George Lucas", "Mark Hamill, Harrison Ford, Carrie Fisher", "A long time ago...", "English", "United States", "Won 6 Oscars", "poster1.jpg", "90", "8.6", "1,234,567", "25.00");

        _mockCinemaWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderMovieDto> { new("cw1", "STAR WARS: EPISODE IV", "movie", "1977", "poster1.jpg") });

        _mockFilmWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderMovieDto>());

        _mockCinemaWorldClient.Setup(x => x.GetMovieDetailsAsync("cw1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cinemaMovie);

        var request = new GetMovieDetailsRequest("Star Wars: Episode IV");

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MovieDetails.Title.Should().Be("Star Wars: Episode IV");
        result.MovieDetails.Year.Should().Be(1977);
    }

    [Fact]
    public async Task Handle_ShouldHandleEmptyOrNullMovieTitles()
    {
        // Arrange
        var cinemaMovie = new ProviderMovieDetailDto("cw1", "Star Wars: Episode IV", "movie", "1977", "PG", "1977-05-25", "121 min", "Action, Adventure, Fantasy", "George Lucas", "George Lucas", "Mark Hamill, Harrison Ford, Carrie Fisher", "A long time ago...", "English", "United States", "Won 6 Oscars", "poster1.jpg", "90", "8.6", "1,234,567", "25.00");

        _mockCinemaWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderMovieDto> 
            { 
                new("cw1", "Star Wars: Episode IV", "movie", "1977", "poster1.jpg"),
                new("cw2", "", "movie", "1999", "poster2.jpg"),
                new("cw3", null!, "movie", "2000", "poster3.jpg")
            });

        _mockFilmWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderMovieDto>());

        _mockCinemaWorldClient.Setup(x => x.GetMovieDetailsAsync("cw1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cinemaMovie);

        var request = new GetMovieDetailsRequest("Star Wars: Episode IV");

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MovieDetails.Title.Should().Be("Star Wars: Episode IV");
        result.MovieDetails.Year.Should().Be(1977);
    }

    [Fact]
    public async Task Handle_ShouldHandleNullValuesInMovieDetails()
    {
        // Arrange
        var cinemaMovie = new ProviderMovieDetailDto("cw1", "Star Wars: Episode IV", "movie", "1977", "PG", "1977-05-25", "121 min", "Action, Adventure, Fantasy", "George Lucas", "George Lucas", "Mark Hamill, Harrison Ford, Carrie Fisher", "A long time ago...", "English", "United States", "Won 6 Oscars", "poster1.jpg", "90", "8.6", "1,234,567", "25.00");

        _mockCinemaWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderMovieDto> { new("cw1", "Star Wars: Episode IV", "movie", "1977", "poster1.jpg") });

        _mockFilmWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderMovieDto>());

        _mockCinemaWorldClient.Setup(x => x.GetMovieDetailsAsync("cw1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cinemaMovie);

        var request = new GetMovieDetailsRequest("Star Wars: Episode IV");

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MovieDetails.Title.Should().Be("Star Wars: Episode IV");
        result.MovieDetails.Year.Should().Be(1977);
        result.MovieDetails.Type.Should().Be("movie");
        result.MovieDetails.PosterUrl.Should().Be("poster1.jpg");
        result.MovieDetails.CinemaWorldPrice.Should().Be(25.00m);
        result.MovieDetails.FilmWorldPrice.Should().BeNull();
    }
} 