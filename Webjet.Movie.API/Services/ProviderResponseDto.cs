namespace Webjet.Movie.API.Services;

public record ProviderMovieDto(
    string ID,
    string Title,
    string Type,
    string Year,
    string Poster
);

public record ProviderMovieDetailDto(
    string ID,
    string Title,
    string Type,
    string Year,
    string Rated,
    string Released,
    string Runtime,
    string Genre,
    string Director,
    string Writer,
    string Actors,
    string Plot,
    string Language,
    string Country,
    string Awards,
    string Poster,
    string Metascore,
    string Rating,
    string Votes,
    string Price
);

public record ProviderMoviesResponse(IEnumerable<ProviderMovieDto> Movies);

public record ProviderMovieDetailResponse(ProviderMovieDetailDto Movie); 