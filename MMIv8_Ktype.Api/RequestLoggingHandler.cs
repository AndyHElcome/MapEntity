using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Models;
using Serilog;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MMIv8_Ktype.Api
{
    public class RequestLoggingHandler(ILogger Log, HttpMessageHandler? innerHandler = null) : DelegatingHandler(innerHandler ?? new HttpClientHandler())
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                Log.Debug("Sending {Method} Request {RequestUri}", request.Method, request.RequestUri);
                var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                    Log.Information("Sent {Method} Request {RequestUri} {StatusCode}", request.Method, request.RequestUri, response.StatusCode);
                else
                    Log.Warning("Sent {Method} Request {RequestUri} {StatusCode} {@content}", request.Method, request.RequestUri, response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
                
                //response.EnsureSuccessStatusCode();
                return response;
            }
            catch (TaskCanceledException ex) //when (!cancellationToken.IsCancellationRequested)
            {
                Log.Warning(ex, "Timeout making {Method} call to {RequestUri} {@options}", request.Method, request.RequestUri, request.Options);
                return new HttpResponseMessage(HttpStatusCode.RequestTimeout); // 408
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error making {Method} call to {RequestUri}", request.Method, request.RequestUri);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }
    }
}
