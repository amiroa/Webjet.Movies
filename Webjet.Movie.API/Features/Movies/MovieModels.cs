namespace Webjet.Movie.API.Features.Movies;

public record MovieSummary(
    string Title, 
    int Year, 
    string Type, 
    string PosterUrl, 
    string? CinemaWorldId, 
    string? FilmWorldId
);

public record MovieDetailsResult(
    string Title,
    int Year,
    string Type,
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
    string PosterUrl,
    string Metascore,
    string Rating,
    string Votes,
    decimal CheapestPrice,
    string CheapestProvider,
    decimal? CinemaWorldPrice,
    decimal? FilmWorldPrice
);

public record PaginationInfo(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage
);

public record GetMoviesRequest(
    int Page = 1, 
    int PageSize = 10, 
    string? SearchTerm = null,
    string? SortBy = null,
    bool SortDescending = false
) : IRequest<GetMoviesResponse>;

public record GetMoviesResponse(
    IEnumerable<MovieSummary> Movies,
    PaginationInfo Pagination
);

public record GetMovieDetailsRequest(string Title) : IRequest<GetMovieDetailsResponse>;

public record GetMovieDetailsResponse(MovieDetailsResult MovieDetails); 