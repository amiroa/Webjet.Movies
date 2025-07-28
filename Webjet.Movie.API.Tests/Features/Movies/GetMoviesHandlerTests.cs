using FluentAssertions;
using Moq;
using Webjet.Movie.API.Features.Movies;
using Webjet.Movie.API.Services;
using Xunit;

namespace Webjet.Movie.API.Tests.Features.Movies;

public class GetMoviesHandlerTests : TestBase
{
    private GetMoviesHandler _handler = null!;
    private Mock<CinemaWorldClient> _mockCinemaWorldClient = null!;
    private Mock<FilmWorldClient> _mockFilmWorldClient = null!;

    public GetMoviesHandlerTests()
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
        _handler = new GetMoviesHandler(providers, GetLogger<GetMoviesHandler>(), GetMemoryCache());
    }

    [Fact]
    public async Task Handle_ShouldReturnMergedMovies_WhenBothProvidersReturnData()
    {
        // Arrange
        var cinemaMovies = new List<ProviderMovieDto>
        {
            new("cw1", "Star Wars: Episode IV", "movie", "1977", "poster1.jpg"),
            new("cw2", "The Matrix", "movie", "1999", "poster2.jpg")
        };

        var filmMovies = new List<ProviderMovieDto>
        {
            new("fw1", "Star Wars: Episode IV", "movie", "1977", "poster1.jpg"),
            new("fw3", "Inception", "movie", "2010", "poster3.jpg")
        };

        _mockCinemaWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cinemaMovies);

        _mockFilmWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(filmMovies);

        var request = new GetMoviesRequest(1, 10, "", "", false);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Movies.Should().HaveCount(3); // 3 unique movies
        result.Pagination.TotalItems.Should().Be(3);
        result.Pagination.Page.Should().Be(1);
        result.Pagination.TotalPages.Should().Be(1);

        var starWars = result.Movies.FirstOrDefault(m => m.Title == "Star Wars: Episode IV");
        starWars.Should().NotBeNull();
        starWars!.CinemaWorldId.Should().Be("cw1");
        starWars.FilmWorldId.Should().Be("fw1");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenBothProvidersReturnEmpty()
    {
        // Arrange
        _mockCinemaWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<ProviderMovieDto>());

        _mockFilmWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<ProviderMovieDto>());

        var request = new GetMoviesRequest(1, 10, "", "", false);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Movies.Should().BeEmpty();
        result.Pagination.TotalItems.Should().Be(0);
        result.Pagination.Page.Should().Be(1);
        result.Pagination.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldApplySearchFilter_WhenSearchTermProvided()
    {
        // Arrange
        var cinemaMovies = new List<ProviderMovieDto>
        {
            new("cw1", "Star Wars: Episode IV", "movie", "1977", "poster1.jpg"),
            new("cw2", "The Matrix", "movie", "1999", "poster2.jpg")
        };

        var filmMovies = new List<ProviderMovieDto>
        {
            new("fw1", "Star Wars: Episode IV", "movie", "1977", "poster1.jpg"),
            new("fw3", "Inception", "movie", "2010", "poster3.jpg")
        };

        _mockCinemaWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cinemaMovies);

        _mockFilmWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(filmMovies);

        var request = new GetMoviesRequest(1, 10, "Star Wars", "", false);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Movies.Should().HaveCount(1);
        result.Movies.First().Title.Should().Be("Star Wars: Episode IV");
    }

    [Fact]
    public async Task Handle_ShouldApplySorting_WhenSortByProvided()
    {
        // Arrange
        var cinemaMovies = new List<ProviderMovieDto>
        {
            new("cw1", "Star Wars: Episode IV", "movie", "1977", "poster1.jpg"),
            new("cw2", "The Matrix", "movie", "1999", "poster2.jpg")
        };

        var filmMovies = new List<ProviderMovieDto>
        {
            new("fw1", "Star Wars: Episode IV", "movie", "1977", "poster1.jpg"),
            new("fw3", "Inception", "movie", "2010", "poster3.jpg")
        };

        _mockCinemaWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cinemaMovies);

        _mockFilmWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(filmMovies);

        var request = new GetMoviesRequest(1, 10, "", "title", false);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Movies.Should().HaveCount(3);
        result.Movies.Select(m => m.Title).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Handle_ShouldApplyDescendingSorting_WhenSortDescendingIsTrue()
    {
        // Arrange
        var cinemaMovies = new List<ProviderMovieDto>
        {
            new("cw1", "Star Wars: Episode IV", "movie", "1977", "poster1.jpg"),
            new("cw2", "The Matrix", "movie", "1999", "poster2.jpg")
        };

        var filmMovies = new List<ProviderMovieDto>
        {
            new("fw1", "Star Wars: Episode IV", "movie", "1977", "poster1.jpg"),
            new("fw3", "Inception", "movie", "2010", "poster3.jpg")
        };

        _mockCinemaWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cinemaMovies);

        _mockFilmWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(filmMovies);

        var request = new GetMoviesRequest(1, 10, "", "title", true);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Movies.Should().HaveCount(3);
        result.Movies.Select(m => m.Title).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task Handle_ShouldApplyPagination_WhenPageSizeIsLessThanTotal()
    {
        // Arrange
        var cinemaMovies = new List<ProviderMovieDto>
        {
            new("cw1", "Star Wars: Episode IV", "movie", "1977", "poster1.jpg"),
            new("cw2", "The Matrix", "movie", "1999", "poster2.jpg")
        };

        var filmMovies = new List<ProviderMovieDto>
        {
            new("fw1", "Star Wars: Episode IV", "movie", "1977", "poster1.jpg"),
            new("fw3", "Inception", "movie", "2010", "poster3.jpg")
        };

        _mockCinemaWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cinemaMovies);

        _mockFilmWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(filmMovies);

        var request = new GetMoviesRequest(1, 2, "", "", false);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Movies.Should().HaveCount(2);
        result.Pagination.TotalItems.Should().Be(3);
        result.Pagination.Page.Should().Be(1);
        result.Pagination.TotalPages.Should().Be(2);
        result.Pagination.HasNextPage.Should().BeTrue();
        result.Pagination.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldHandleCaseInsensitiveTitleMatching()
    {
        // Arrange
        var cinemaMovies = new List<ProviderMovieDto>
        {
            new("cw1", "Star Wars: Episode IV", "movie", "1977", "poster1.jpg"),
            new("cw2", "The Matrix", "movie", "1999", "poster2.jpg")
        };

        var filmMovies = new List<ProviderMovieDto>
        {
            new("fw1", "STAR WARS: EPISODE IV", "movie", "1977", "poster1.jpg"),
            new("fw3", "Inception", "movie", "2010", "poster3.jpg")
        };

        _mockCinemaWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cinemaMovies);

        _mockFilmWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(filmMovies);

        var request = new GetMoviesRequest(1, 10, "", "", false);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Movies.Should().HaveCount(3);
        var starWars = result.Movies.FirstOrDefault(m => m.Title == "Star Wars: Episode IV");
        starWars.Should().NotBeNull();
        starWars!.CinemaWorldId.Should().Be("cw1");
        starWars.FilmWorldId.Should().Be("fw1");
    }

    [Fact]
    public async Task Handle_ShouldHandleEmptyOrNullValues()
    {
        // Arrange
        var cinemaMovies = new List<ProviderMovieDto>
        {
            new("cw1", "Star Wars: Episode IV", "movie", "1977", "poster1.jpg"),
            new("cw2", "", "movie", "1999", "poster2.jpg"),
            new("cw3", null!, "movie", "2000", "poster3.jpg")
        };

        var filmMovies = new List<ProviderMovieDto>
        {
            new("fw1", "Star Wars: Episode IV", "movie", "1977", "poster1.jpg"),
            new("fw2", "", "movie", "1999", "poster2.jpg"),
            new("fw3", null!, "movie", "2000", "poster3.jpg")
        };

        _mockCinemaWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cinemaMovies);

        _mockFilmWorldClient.Setup(x => x.GetMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(filmMovies);

        var request = new GetMoviesRequest(1, 10, "", "", false);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Movies.Should().HaveCount(1); // Only Star Wars should be included
        result.Movies.First().Title.Should().Be("Star Wars: Episode IV");
    }
} 