using Microsoft.AspNetCore.Http;
using MMIv8_Ktype.Models;

namespace MMIv8_Ktype.Api
{
    public class ResultEndpointFilter : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var result = await next(context);

            if (result is IResultWrapper wrapper && !wrapper.IsSuccess)
                return wrapper.ToProblemDetails();
                ////return Results.Problem(
                ////    detail: wrapper.Error?.Description,
                ////    statusCode: 400,
                ////    title: "Request failed");
            else
                return result;
        }
    }
}
