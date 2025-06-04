using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Refit;
using Serilog;
using System.Diagnostics;
using MMIv8_Ktype.Models;

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
}
