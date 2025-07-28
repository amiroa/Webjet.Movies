namespace Webjet.Movie.API.Features.Movies;

public class GetMoviesModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/movies", async (
            IMediator mediator, 
            int page = 1, 
            int pageSize = 10,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false) =>
        {
            var request = new GetMoviesRequest(page, pageSize, searchTerm, sortBy, sortDescending);
            var response = await mediator.Send(request);
            return Results.Ok(response);
        });
    }
} 