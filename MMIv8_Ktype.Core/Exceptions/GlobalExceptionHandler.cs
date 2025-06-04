using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Refit;
using Serilog;
using System.Diagnostics;

namespace MMIv8_Ktype.Core.Exceptions
{
    public class GlobalExceptionHandler(Serilog.ILogger log, IProblemDetailsService problemDetailsService) : IExceptionHandler
    {
        private readonly Serilog.ILogger _log = log;
        private readonly IProblemDetailsService _problemDetailsService = problemDetailsService;

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            _log.Error(exception, "Exception Message: {message}", exception.Message);

            httpContext.Response.StatusCode = exception switch
            {
                ApplicationException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError,
            };

            Activity? activity = httpContext.Features.Get<IHttpActivityFeature>()?.Activity;

            return await _problemDetailsService.TryWriteAsync(
                new ProblemDetailsContext
                {
                    HttpContext = httpContext,
                    Exception = exception,
                    ProblemDetails = new()
                    {
                        Type = exception.GetType().Name,
                        Title = "An error occured",
                        Detail = exception.Message,
                        Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}",
                        Extensions = new Dictionary<string, object?>
                        {
                            { "requestId", httpContext.TraceIdentifier },
                            { "traceId", activity?.Id },
                        }
                    }
                });
        }
    }

    public class ExceptionHandlingMiddleware(Serilog.ILogger log, RequestDelegate next)
    {
        private readonly Serilog.ILogger _log = log;
        private readonly RequestDelegate _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                _log.Error(exception, "Exception occurred: {Message}", exception.Message);

                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Server Error",
                    Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1"
                };

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                await context.Response.WriteAsJsonAsync(problemDetails);
            }
        }
    }
}
