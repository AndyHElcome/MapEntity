using Serilog;

namespace MMIv8_Ktype.Api
{
    public class RequestLoggingHandler(ILogger Log, HttpMessageHandler? innerHandler = null) : DelegatingHandler(innerHandler ?? new HttpClientHandler())
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                Log.Information("Sending {Method} Request {RequestUri}", request.Method, request.RequestUri);
                var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                return response;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error making {Method} call to {RequestUri}", request.Method, request.RequestUri);
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
            }
        }
    }
}
