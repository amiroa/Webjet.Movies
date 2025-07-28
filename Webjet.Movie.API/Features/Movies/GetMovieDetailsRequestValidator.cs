namespace Webjet.Movie.API.Features.Movies;

public class GetMovieDetailsRequestValidator : AbstractValidator<GetMovieDetailsRequest>
{
    public GetMovieDetailsRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Movie title is required")
            .MaximumLength(200)
            .WithMessage("Movie title cannot exceed 200 characters");
    }
} 