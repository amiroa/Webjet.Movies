using Microsoft.Extensions.Options;
using System.Net;

namespace Webjet.Movie.API.Services;

public class FilmWorldClient : IProviderClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FilmWorldClient> _logger;
    private readonly IMemoryCache _cache;
    private readonly IAsyncPolicy<HttpResponseMessage> _resiliencePolicy;
    private readonly string _apiKey;
    private readonly int _moviesCacheMinutes;
    private readonly int _movieDetailsCacheMinutes;
    private readonly int _timeoutSeconds;
    private readonly int _retryCount;
    private readonly int _circuitBreakerFailures;
    private readonly int _circuitBreakerDurationSeconds;

    public string ProviderName => "FilmWorld";

    public FilmWorldClient(
        HttpClient httpClient,
        ILogger<FilmWorldClient> logger,
        IMemoryCache cache,
        IOptions<MovieProvidersConfig> providersConfig,
        IOptions<CacheSettings> cacheSettings)
    {
        _httpClient = httpClient;
        var config = providersConfig.Value.Filmworld;
        _httpClient.BaseAddress = new Uri(config.BaseUrl);
        _logger = logger;
        _cache = cache;
        _apiKey = config.ApiKey;
        _moviesCacheMinutes = cacheSettings.Value.MoviesCacheMinutes;
        _movieDetailsCacheMinutes = cacheSettings.Value.MovieDetailsCacheMinutes;
        _timeoutSeconds = config.TimeoutSeconds;
        _retryCount = config.RetryCount;
        _circuitBreakerFailures = config.CircuitBreakerFailures;
        _circuitBreakerDurationSeconds = config.CircuitBreakerDurationSeconds;
        _resiliencePolicy = CreateResiliencePolicy();
    }

    public virtual async Task<IEnumerable<ProviderMovieDto>> GetMoviesAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{ProviderName}_movies";
        
        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out IEnumerable<ProviderMovieDto>? cachedMovies))
        {
            _logger.LogInformation("Returning cached movies from {Provider}", ProviderName);
            return cachedMovies!;
        }

        try
        {
            // Create a new request with explicit headers
            var request = new HttpRequestMessage(HttpMethod.Get, "movies");
            request.Headers.Add("x-access-token", _apiKey);
            
            var response = await _resiliencePolicy.ExecuteAsync(async () =>
                await _httpClient.SendAsync(request, cancellationToken));

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var moviesResponse = JsonSerializer.Deserialize<ProviderMoviesResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var movies = moviesResponse?.Movies ?? Enumerable.Empty<ProviderMovieDto>();
                
                // Cache the result using configured time
                _cache.Set(cacheKey, movies, TimeSpan.FromMinutes(_moviesCacheMinutes));
                
                _logger.LogInformation("Successfully fetched {Count} movies from {Provider}", movies.Count(), ProviderName);
                return movies;
            }

            _logger.LogWarning("Failed to fetch movies from {Provider}. Status: {StatusCode}", ProviderName, response.StatusCode);
            return Enumerable.Empty<ProviderMovieDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching movies from {Provider}", ProviderName);
            return Enumerable.Empty<ProviderMovieDto>();
        }
    }

    public virtual async Task<ProviderMovieDetailDto?> GetMovieDetailsAsync(string id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{ProviderName}_movie_{id}";
        
        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out ProviderMovieDetailDto? cachedMovie))
        {
            _logger.LogInformation("Returning cached movie details from {Provider} for ID: {Id}", ProviderName, id);
            return cachedMovie;
        }

        try
        {
            // Create a new request with explicit headers
            var request = new HttpRequestMessage(HttpMethod.Get, $"movie/{id}");
            request.Headers.Add("x-access-token", _apiKey);
            
            var response = await _resiliencePolicy.ExecuteAsync(async () =>
                await _httpClient.SendAsync(request, cancellationToken));

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var movie = JsonSerializer.Deserialize<ProviderMovieDetailDto>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (movie != null && !string.IsNullOrEmpty(movie.ID))
                {
                    // Cache the result using configured time
                    _cache.Set(cacheKey, movie, TimeSpan.FromMinutes(_movieDetailsCacheMinutes));
                    
                    _logger.LogInformation("Successfully fetched movie details from {Provider} for ID: {Id}", ProviderName, id);
                    return movie;
                }
                else
                {
                    _logger.LogWarning("No movie details found in {Provider} for ID: {Id}", ProviderName, id);
                    return null;
                }
            }

            _logger.LogWarning("Failed to fetch movie details from {Provider} for ID: {Id}. Status: {StatusCode}", ProviderName, id, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching movie details from {Provider} for ID: {Id}", ProviderName, id);
            return null;
        }
    }

    private IAsyncPolicy<HttpResponseMessage> CreateResiliencePolicy()
    {
        // Timeout policy
        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(_timeoutSeconds));

        // Retry policy with exponential backoff and jitter
        var retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(_retryCount, attempt => 
                TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)) + 
                TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)));

        // Circuit breaker policy
        var circuitBreakerPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(r => !r.IsSuccessStatusCode)
            .CircuitBreakerAsync(_circuitBreakerFailures, TimeSpan.FromSeconds(_circuitBreakerDurationSeconds));

        // Fallback policy - return service unavailable response
        var fallbackPolicy = Policy<HttpResponseMessage>
            .Handle<Exception>()
            .OrResult(r => !r.IsSuccessStatusCode)
            .FallbackAsync((context) =>
            {
                _logger.LogWarning("Using fallback for {Provider} request", ProviderName);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    ReasonPhrase = $"{ProviderName} unavailable and no cache available."
                });
            });

        // Wrap policies in order: fallback -> circuit breaker -> retry -> timeout
        return Policy.WrapAsync(fallbackPolicy, circuitBreakerPolicy, retryPolicy, timeoutPolicy);
    }
} 