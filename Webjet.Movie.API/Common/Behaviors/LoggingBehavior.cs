using System.Diagnostics;

namespace Webjet.Movie.API.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse>
    (ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
    where TResponse : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        logger.LogInformation("[START] Handle request={Request} - Response={Response} - RequestData={RequestData}",
            typeof(TRequest).Name, typeof(TResponse).Name, request);

        var timer = new Stopwatch();
        timer.Start();

        try
        {
            var response = await next();
            timer.Stop();
            var timeTaken = timer.Elapsed;
            if (timeTaken.TotalSeconds > 3) // if the request is greater than 3 seconds, then log the warnings
                logger.LogWarning("[PERFORMANCE] The request {Request} took {TimeTaken} seconds.",
                    typeof(TRequest).Name, timeTaken.TotalSeconds);

            logger.LogInformation("[END] Handled {Request} with {Response}", typeof(TRequest).Name, typeof(TResponse).Name);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling {Request}", typeof(TRequest).Name);
            throw;
        }
    }
} 