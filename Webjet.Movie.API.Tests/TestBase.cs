using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Webjet.Movie.API.Services;

namespace Webjet.Movie.API.Tests;

public abstract class TestBase
{
    protected IServiceProvider ServiceProvider { get; private set; } = null!;

    protected TestBase()
    {
        SetupServices();
    }

    private void SetupServices()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Add memory cache
        services.AddMemoryCache();

        // Configure test settings
        services.Configure<MovieProvidersConfig>(options =>
        {
            options.Cinemaworld = new ProviderConfig
            {
                BaseUrl = "https://test-cinemaworld.com/",
                ApiKey = "test-cinema-key"
            };
            options.Filmworld = new ProviderConfig
            {
                BaseUrl = "https://test-filmworld.com/",
                ApiKey = "test-film-key"
            };
        });

        services.Configure<CacheSettings>(options =>
        {
            options.MoviesCacheMinutes = 5;
            options.MovieDetailsCacheMinutes = 10;
        });

        ServiceProvider = services.BuildServiceProvider();
    }

    protected ILogger<T> GetLogger<T>()
    {
        return ServiceProvider.GetRequiredService<ILogger<T>>();
    }

    protected IOptions<MovieProvidersConfig> GetProvidersConfig()
    {
        return ServiceProvider.GetRequiredService<IOptions<MovieProvidersConfig>>();
    }

    protected IOptions<CacheSettings> GetCacheSettings()
    {
        return ServiceProvider.GetRequiredService<IOptions<CacheSettings>>();
    }

    protected IMemoryCache GetMemoryCache()
    {
        return ServiceProvider.GetRequiredService<IMemoryCache>();
    }
} 