using Webjet.Movie.API.Services;

namespace Webjet.Movie.API.Features.Movies;

public class GetMoviesHandler : IRequestHandler<GetMoviesRequest, GetMoviesResponse>
{
    private readonly IProviderClient _cinemaWorldClient;
    private readonly IProviderClient _filmWorldClient;
    private readonly ILogger<GetMoviesHandler> _logger;
    private readonly IMemoryCache _cache;

    public GetMoviesHandler(
        IEnumerable<IProviderClient> providers,
        ILogger<GetMoviesHandler> logger,
        IMemoryCache cache)
    {
        _cinemaWorldClient = providers.First(p => p.ProviderName.ToLowerInvariant() == "cinemaworld");
        _filmWorldClient = providers.First(p => p.ProviderName.ToLowerInvariant() == "filmworld");
        _logger = logger;
        _cache = cache;
    }

    public async Task<GetMoviesResponse> Handle(GetMoviesRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching movies from all providers with pagination - Page: {Page}, PageSize: {PageSize}, Search: {SearchTerm}, SortBy: {SortBy}, SortDesc: {SortDescending}", 
            request.Page, request.PageSize, request.SearchTerm, request.SortBy, request.SortDescending);

        // Get movies (from cache or fetch from providers)
        var allMovies = await GetMoviesList(cancellationToken);
        
        // Apply filters, sorting, and pagination
        return ProcessMoviesRequest(allMovies, request);
    }

    private async Task<List<MovieSummary>> GetMoviesList(CancellationToken cancellationToken)
    {
        // Try to get processed movies from cache first
        var cacheKey = "processed_movies_list";
        if (_cache.TryGetValue(cacheKey, out List<MovieSummary>? cachedMovies) && cachedMovies != null)
        {
            _logger.LogInformation("Using cached processed movies list with {Count} movies", cachedMovies.Count);
            return cachedMovies;
        }

        // Fetch from both providers concurrently
        var cinemaWorldMoviesTask = _cinemaWorldClient.GetMoviesAsync(cancellationToken);
        var filmWorldMoviesTask = _filmWorldClient.GetMoviesAsync(cancellationToken);
        await Task.WhenAll(cinemaWorldMoviesTask, filmWorldMoviesTask);
        
        var cinemaWorldMovies = cinemaWorldMoviesTask.Result;
        var filmWorldMovies = filmWorldMoviesTask.Result;

        _logger.LogInformation("Provider {Provider} returned {Count} movies", _cinemaWorldClient.ProviderName, cinemaWorldMovies.Count());
        _logger.LogInformation("Provider {Provider} returned {Count} movies", _filmWorldClient.ProviderName, filmWorldMovies.Count());

        // Merge results by movie title
        var mergedMovies = MergeMoviesFromProviders(cinemaWorldMovies, filmWorldMovies);
        
        // Cache the processed movies list
        _cache.Set(cacheKey, mergedMovies, TimeSpan.FromMinutes(5));
        _logger.LogInformation("Cached processed movies list with {Count} movies", mergedMovies.Count);
        
        return mergedMovies;
    }

    private static List<MovieSummary> MergeMoviesFromProviders(IEnumerable<ProviderMovieDto> cinemaWorldMovies, IEnumerable<ProviderMovieDto> filmWorldMovies)
    {
        var movieDictionary = new Dictionary<string, MovieSummary>(StringComparer.OrdinalIgnoreCase);

        // Process CinemaWorld movies
        foreach (var movie in cinemaWorldMovies)
        {
            if (string.IsNullOrWhiteSpace(movie.Title))
                continue;
                
            movieDictionary[movie.Title.Trim()] = new MovieSummary(
                movie.Title,
                int.TryParse(movie.Year, out var year) ? year : 0,
                movie.Type,
                movie.Poster,
                CinemaWorldId: movie.ID,
                FilmWorldId: null
            );
        }

        // Process FilmWorld movies and merge with existing ones
        foreach (var movie in filmWorldMovies)
        {
            if (string.IsNullOrWhiteSpace(movie.Title))
                continue;
                
            if (movieDictionary.TryGetValue(movie.Title.Trim(), out var existing))
            {
                movieDictionary[movie.Title.Trim()] = existing with { FilmWorldId = movie.ID };
            }
            else
            {
                movieDictionary[movie.Title.Trim()] = new MovieSummary(
                    movie.Title,
                    int.TryParse(movie.Year, out var year) ? year : 0,
                    movie.Type,
                    movie.Poster,
                    CinemaWorldId: null,
                    FilmWorldId: movie.ID
                );
            }
        }

        return movieDictionary.Values.ToList();
    }

    private GetMoviesResponse ProcessMoviesRequest(List<MovieSummary> allMovies, GetMoviesRequest request)
    {
        var processedMovies = allMovies;

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            processedMovies = processedMovies
                .Where(m => m.Title.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
            _logger.LogInformation("Applied search filter for '{SearchTerm}'. Filtered movies: {Count}", 
                request.SearchTerm, processedMovies.Count);
        }
        
        // Apply sorting
        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            processedMovies = ApplySorting(processedMovies, request.SortBy, request.SortDescending);
            _logger.LogInformation("Applied sorting by '{SortBy}' {Direction}", 
                request.SortBy, request.SortDescending ? "descending" : "ascending");
        }

        // Apply pagination
        var (paginatedMovies, paginationInfo) = ApplyPagination(processedMovies, request.Page, request.PageSize);
        
        _logger.LogInformation("Pagination applied: Page {Page}/{TotalPages}, Items {ItemsOnPage}/{TotalItems}", 
            request.Page, paginationInfo.TotalPages, paginatedMovies.Count, paginationInfo.TotalItems);

        return new GetMoviesResponse(paginatedMovies, paginationInfo);
    }

    private static List<MovieSummary> ApplySorting(List<MovieSummary> movies, string sortBy, bool sortDescending)
    {
        return sortBy.ToLowerInvariant() switch
        {
            "title" => sortDescending 
                ? movies.OrderByDescending(m => m.Title).ToList()
                : movies.OrderBy(m => m.Title).ToList(),
            "year" => sortDescending 
                ? movies.OrderByDescending(m => m.Year).ToList()
                : movies.OrderBy(m => m.Year).ToList(),
            _ => movies // Default: no sorting
        };
    }

    private static (List<MovieSummary> PaginatedMovies, PaginationInfo PaginationInfo) ApplyPagination(List<MovieSummary> movies, int page, int pageSize)
    {
        var totalItems = movies.Count;
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        var skip = (page - 1) * pageSize;
        var paginatedMovies = movies.Skip(skip).Take(pageSize).ToList();

        var paginationInfo = new PaginationInfo(
            page,
            pageSize,
            totalItems,
            totalPages,
            page < totalPages,
            page > 1
        );

        return (paginatedMovies, paginationInfo);
    }
} 