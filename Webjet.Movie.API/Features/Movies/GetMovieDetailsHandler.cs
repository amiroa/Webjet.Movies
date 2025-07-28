using Webjet.Movie.API.Common.Exceptions;
using Webjet.Movie.API.Services;

namespace Webjet.Movie.API.Features.Movies;

public class GetMovieDetailsHandler : IRequestHandler<GetMovieDetailsRequest, GetMovieDetailsResponse>
{
    private readonly IProviderClient _cinemaWorldClient;
    private readonly IProviderClient _filmWorldClient;
    private readonly ILogger<GetMovieDetailsHandler> _logger;

    public GetMovieDetailsHandler(
        IEnumerable<IProviderClient> providers,
        ILogger<GetMovieDetailsHandler> logger)
    {
        _cinemaWorldClient = providers.First(p => p.ProviderName.ToLowerInvariant() == "cinemaworld");
        _filmWorldClient = providers.First(p => p.ProviderName.ToLowerInvariant() == "filmworld");
        _logger = logger;
    }

    public async Task<GetMovieDetailsResponse> Handle(GetMovieDetailsRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching movie details for: {Title}", request.Title);

        // Get cached movies from each provider
        var cinemaWorldMoviesTask = _cinemaWorldClient.GetMoviesAsync(cancellationToken);
        var filmWorldMoviesTask = _filmWorldClient.GetMoviesAsync(cancellationToken);
        await Task.WhenAll(cinemaWorldMoviesTask, filmWorldMoviesTask);
        var cinemaWorldMovies = cinemaWorldMoviesTask.Result;
        var filmWorldMovies = filmWorldMoviesTask.Result;

        var detailTasks = new List<Task<(string ProviderName, ProviderMovieDetailDto? Detail)>>();

        var cinemaMovie = cinemaWorldMovies.FirstOrDefault(m => string.Equals(m.Title.Trim(), request.Title.Trim(), StringComparison.OrdinalIgnoreCase));
        if (cinemaMovie != null && !string.IsNullOrEmpty(cinemaMovie.ID))
        {
            _logger.LogInformation("Found movie '{Title}' in {Provider} with ID: {Id}", request.Title, _cinemaWorldClient.ProviderName, cinemaMovie.ID);
            detailTasks.Add(GetMovieDetailAsync(_cinemaWorldClient, cinemaMovie.ID, cancellationToken));
        }
        else
        {
            _logger.LogWarning("Movie '{Title}' not found in {Provider}", request.Title, _cinemaWorldClient.ProviderName);
        }

        var filmMovie = filmWorldMovies.FirstOrDefault(m => string.Equals(m.Title.Trim(), request.Title.Trim(), StringComparison.OrdinalIgnoreCase));
        if (filmMovie != null && !string.IsNullOrEmpty(filmMovie.ID))
        {
            _logger.LogInformation("Found movie '{Title}' in {Provider} with ID: {Id}", request.Title, _filmWorldClient.ProviderName, filmMovie.ID);
            detailTasks.Add(GetMovieDetailAsync(_filmWorldClient, filmMovie.ID, cancellationToken));
        }
        else
        {
            _logger.LogWarning("Movie '{Title}' not found in {Provider}", request.Title, _filmWorldClient.ProviderName);
        }

        if (!detailTasks.Any())
            throw new MovieNotFoundException(request.Title);

        // Run the detail fetches in parallel
        var detailResults = await Task.WhenAll(detailTasks);
        var availableDetails = detailResults.Where(r => r.Detail != null).ToList();

        _logger.LogInformation("Movie details fetch results for '{Title}': {AvailableCount} successful, {TotalCount} total attempts", 
            request.Title, availableDetails.Count, detailResults.Length);

        if (!availableDetails.Any())
        {
            var failedProviders = detailResults.Where(r => r.Detail == null).Select(r => r.ProviderName);
            _logger.LogWarning("All providers failed for movie '{Title}'. Failed providers: {FailedProviders}", 
                request.Title, string.Join(", ", failedProviders));
            
            throw new ExternalServiceException("Failed to retrieve movie details from all providers");
        }

        // Find the cheapest price
        var cheapestDetail = availableDetails
            .Where(r => r.Detail != null)
            .OrderBy(r => decimal.TryParse(r.Detail!.Price, out var price) ? price : decimal.MaxValue)
            .First();

        // Use the first available detail for movie information (they should be the same)
        var movieDetail = cheapestDetail.Detail!;
        var cinemaWorldDetail = availableDetails.FirstOrDefault(r => r.ProviderName == _cinemaWorldClient.ProviderName).Detail;
        var filmWorldDetail = availableDetails.FirstOrDefault(r => r.ProviderName == _filmWorldClient.ProviderName).Detail;

        var result = new MovieDetailsResult(
            Title: GetBestValue(movieDetail.Title, availableDetails.Select(d => d.Detail?.Title)),
            Year: int.TryParse(GetBestValue(movieDetail.Year, availableDetails.Select(d => d.Detail?.Year)), out var year) ? year : 0,
            Type: GetBestValue(movieDetail.Type, availableDetails.Select(d => d.Detail?.Type)),
            Rated: GetBestValue(movieDetail.Rated, availableDetails.Select(d => d.Detail?.Rated)),
            Released: GetBestValue(movieDetail.Released, availableDetails.Select(d => d.Detail?.Released)),
            Runtime: GetBestValue(movieDetail.Runtime, availableDetails.Select(d => d.Detail?.Runtime)),
            Genre: GetBestValue(movieDetail.Genre, availableDetails.Select(d => d.Detail?.Genre)),
            Director: GetBestValue(movieDetail.Director, availableDetails.Select(d => d.Detail?.Director)),
            Writer: GetBestValue(movieDetail.Writer, availableDetails.Select(d => d.Detail?.Writer)),
            Actors: GetBestValue(movieDetail.Actors, availableDetails.Select(d => d.Detail?.Actors)),
            Plot: GetBestValue(movieDetail.Plot, availableDetails.Select(d => d.Detail?.Plot)),
            Language: GetBestValue(movieDetail.Language, availableDetails.Select(d => d.Detail?.Language)),
            Country: GetBestValue(movieDetail.Country, availableDetails.Select(d => d.Detail?.Country)),
            Awards: GetBestValue(movieDetail.Awards, availableDetails.Select(d => d.Detail?.Awards)),
            PosterUrl: GetBestValue(movieDetail.Poster, availableDetails.Select(d => d.Detail?.Poster)),
            Metascore: GetBestValue(movieDetail.Metascore, availableDetails.Select(d => d.Detail?.Metascore)),
            Rating: GetBestValue(movieDetail.Rating, availableDetails.Select(d => d.Detail?.Rating)),
            Votes: GetBestValue(movieDetail.Votes, availableDetails.Select(d => d.Detail?.Votes)),
            CheapestPrice: decimal.TryParse(cheapestDetail.Detail?.Price, out var cheapestPrice) ? cheapestPrice : 0,
            CheapestProvider: cheapestDetail.ProviderName,
            CinemaWorldPrice: decimal.TryParse(cinemaWorldDetail?.Price, out var cwp) ? cwp : (decimal?)null,
            FilmWorldPrice: decimal.TryParse(filmWorldDetail?.Price, out var fwp) ? fwp : (decimal?)null
        );

        return new GetMovieDetailsResponse(result);
    }

    private async Task<(string ProviderName, ProviderMovieDetailDto? Detail)> GetMovieDetailAsync(
        IProviderClient client, string id, CancellationToken cancellationToken)
    {
        try
        {
            var detail = await client.GetMovieDetailsAsync(id, cancellationToken);
            return (client.ProviderName, detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get movie details from {Provider} for ID: {Id}", client.ProviderName, id);
            return (client.ProviderName, null);
        }
    }

    private string GetBestValue(string? currentValue, IEnumerable<string?> availableValues)
    {
        if (!string.IsNullOrWhiteSpace(currentValue))
            return currentValue;

        return availableValues.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
    }
} 