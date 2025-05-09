using Serilog;
using System.Net;

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
            catch (TaskCanceledException ex) //when (!cancellationToken.IsCancellationRequested)
            {
                Log.Warning(ex, "Timeout making {Method} call to {RequestUri} {@options}", request.Method, request.RequestUri, request.Options);
                return new HttpResponseMessage(HttpStatusCode.RequestTimeout); // 408
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error making {Method} call to {RequestUri} {@options}", request.Method, request.RequestUri, request.Options);
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
            }
        }
    }
}
