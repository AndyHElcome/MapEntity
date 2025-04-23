using MMIv8_Ktype.Api.Endpoints;

namespace MMIv8_Ktype.Api
{
    public class RequestPropertyHandler(HttpMessageHandler? innerHandler = null) : DelegatingHandler(innerHandler ?? new HttpClientHandler())
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Get the type of the target interface
            Type interfaceType = (Type?)request.Options.GetValueOrDefault("Refit.InterfaceType") ?? throw new NullReferenceException();

            var builder = new UriBuilder(request.RequestUri ?? throw new NullReferenceException());
            builder.Path = $"/{interfaceType.GetGroupName()}{builder.Path}";

            request.RequestUri = builder.Uri;

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
