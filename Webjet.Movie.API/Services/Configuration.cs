namespace Webjet.Movie.API.Services;

public class MovieProvidersConfig
{
    public ProviderConfig Cinemaworld { get; set; } = new();
    public ProviderConfig Filmworld { get; set; } = new();
}

public class ProviderConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    // Resilience policy settings
    public int TimeoutSeconds { get; set; } = 3;
    public int RetryCount { get; set; } = 3;
    public int CircuitBreakerFailures { get; set; } = 3;
    public int CircuitBreakerDurationSeconds { get; set; } = 30;
}

public class CacheSettings
{
    public int MoviesCacheMinutes { get; set; } = 5;
    public int MovieDetailsCacheMinutes { get; set; } = 10;
} 