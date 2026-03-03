using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Util;
using Refit;
using Serilog;

namespace MMIv8_Ktype.Api
{
    public class RefitClient //Maybe move to Models with IOperation
    {
        private readonly ILogger _log;
        private readonly HttpClient _httpClient;
        private readonly RefitSettings _refitSettings = new();
        public IVersionProvider VersionProvider;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="log"></param>
        /// <param name="timeout">HttpClient Timeout in Minutes</param>
        public RefitClient(ILogger log, int timeout = 1)
        {
            _log = log;

            _httpClient = new HttpClient( //TODO Get HttpClientSettings from Appsettings and pass into constructor
                new RequestPropertyHandler(new RequestLoggingHandler(_log)))
            {
                BaseAddress = new Uri("https://localhost:7083/"),
                //BaseAddress = new Uri("http://dev-sql.headoffice.elcome.co.uk:5700/"),
            };

            _httpClient.Timeout = TimeSpan.FromMinutes(timeout);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");

            _refitSettings = _refitSettings.GetApplyRefitSettings();

            //VersionProvider = new VersionProviderApi(this);
        }

        public void InitialiseVersionProvider()
        {
            VersionProvider = new VersionProviderApi(this);
        }

        public T CreateService<T>()
            where T : IEndpoint
        {
            return RestService.For<T>(_httpClient, _refitSettings);
        }
    }
}
