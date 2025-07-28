using Carter;
using MediatR;

namespace Webjet.Movie.API.Features.Movies;

public class GetMovieDetailsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/movies/details", async (IMediator mediator, string title) =>
        {
            var request = new GetMovieDetailsRequest(title);
            var response = await mediator.Send(request);
            return Results.Ok(response);
        });
    }
} 