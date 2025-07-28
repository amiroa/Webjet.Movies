namespace Webjet.Movie.API.Services;

public interface IProviderClient
{
    Task<IEnumerable<ProviderMovieDto>> GetMoviesAsync(CancellationToken cancellationToken = default);
    Task<ProviderMovieDetailDto?> GetMovieDetailsAsync(string id, CancellationToken cancellationToken = default);
    string ProviderName { get; }
} 