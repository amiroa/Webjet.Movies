namespace Webjet.Movie.API.Common.Exceptions;

public class ExternalServiceException : Exception
{
    public ExternalServiceException(string message) : base(message) { }
    public ExternalServiceException(string message, Exception innerException) : base(message, innerException) { }
}

public class MovieNotFoundException : Exception
{
    public MovieNotFoundException(string title) : base($"Movie '{title}' not found") { }
} 