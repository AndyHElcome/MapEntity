using MMIv8_Ktype.Models.Util;
using MMIv8_Ktype.Models.Collections;
using System.Net.Http.Json;
using Serilog;
using MongoDB.Bson;
using System.Text.Json;
using MongoDB.Driver;
using MMIv8_Ktype.Api.Requests;

// minimal endpoint https://youtu.be/gsAuFIhXz3g?si=MfaGxzKFgLlgWIbR
// reflection endpoint mapping https://youtu.be/CkGFV5bekbY?si=GkVIYuPIObrZDMu1
namespace MMIv8_Ktype.Api
{
    [Obsolete]
    public sealed class MMIv8_KtypeService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger Log;
        private readonly JsonSerializerOptions jsonSerializerOptions = new();

        public MMIv8_KtypeService(HttpClient httpClient, ILogger log)
        {
            _httpClient = httpClient;
            Log = log;
            jsonSerializerOptions = jsonSerializerOptions.GetJsonSerializerOptions();
        }

        #region MakeModelMatch
        public async Task<ObjectId?> CreateMakeModelMatch(MatchMakeModelRequest request)
        {
            return await PostRequestHandler<ObjectId>("MatchMakeModel/CreateMakeModelMatch", request);
        }

        public async Task<MMIv8_Ktype.Models.Collections.MatchMakeModel?> GetMakeModelMatch(MatchMakeModelRequest request)
        {
            return await PostRequestHandler<MMIv8_Ktype.Models.Collections.MatchMakeModel>("MatchMakeModel/GetMakeModelMatch", request);
        }

        public async Task<MatchMakeModel?> GetMakeModelMatchById(ObjectId MatchID)
        {
            return await PostRequestHandler<MMIv8_Ktype.Models.Collections.MatchMakeModel>($"MatchMakeModel/GetMakeModelMatchById/{MatchID}");
        }

        public async Task<ObjectId> DeleteMakeModelMatch(ObjectId MatchID)
        {
            return await PostRequestHandler<ObjectId>($"MatchMakeModel/DeleteMakeModelMatch/{MatchID}");
        }

        public async Task<List<MatchMakeModel>?> GenerateModelMatch()
        {
            return await PostRequestHandler<List<MatchMakeModel>>($"MatchMakeModel/GenerateModelMatch");
        }
        #endregion


        #region Version
        public async Task<IAsyncCursor<MMIv8_Ktype.Models.Collections.Version>?> GetAll()
        {
            return await PostRequestHandler<IAsyncCursor<MMIv8_Ktype.Models.Collections.Version>>("Version/GetAll");
        }

        public async Task<MMIv8_Ktype.Models.Collections.Version?> GetByVersion(int VersionNumber)
        {
            return await PostRequestHandler<MMIv8_Ktype.Models.Collections.Version>($"Version/GetByVersion/{VersionNumber}");
        }

        public async Task<MMIv8_Ktype.Models.Collections.Version?> GetCurrentVersion()
        {
            return await PostRequestHandler<MMIv8_Ktype.Models.Collections.Version>("Version/CurrentVersion");
        }

        public async Task<MMIv8_Ktype.Models.Collections.Version?> CreateVersion(CreateVersionRequest request)
        {
            return await PostRequestHandler<MMIv8_Ktype.Models.Collections.Version>("Version/CreateVersion", request);
        }
        #endregion



        private async Task<TResponse?> PostRequestHandler<TResponse>(string endpoint)
        {
            try
            {
                Log.Information("Making POST call to {URI} {request} {response}", _httpClient.BaseAddress + endpoint, typeof(TResponse));
                var response = await _httpClient.PostAsJsonAsync(endpoint, "", jsonSerializerOptions);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TResponse>(jsonSerializerOptions);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error making POST call to {URI} {request} {response}", _httpClient.BaseAddress + endpoint, typeof(TResponse));
                return default;
            }
        }

        private async Task<TResponse?> PostRequestHandler<TResponse>(string endpoint, IRequest request)
        {
            try
            {
                Log.Information("Making POST call to {URI} {request} {response}", _httpClient.BaseAddress + endpoint, typeof(IRequest), typeof(TResponse));
                var response = await _httpClient.PostAsJsonAsync(endpoint, request, jsonSerializerOptions);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TResponse>(jsonSerializerOptions);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error making POST call to {URI} {request} {response}", _httpClient.BaseAddress + endpoint, typeof(IRequest), typeof(TResponse));
                return default;
            }
        }

        private async Task<TResponse?> GetRequestHandler<TResponse>(string endpoint)
        {
            try
            {
                Log.Information("Making GET call to {URI} {response}", _httpClient.BaseAddress + endpoint, typeof(TResponse));
                return await _httpClient.GetFromJsonAsync<TResponse>(endpoint, jsonSerializerOptions);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error GET POST call to {URI} {response}", _httpClient.BaseAddress + endpoint, typeof(TResponse));
                return default;
            }
        }

        private async Task<TResponse?> DeleteRequestHandler<TResponse>(string endpoint)
        {
            try
            {
                Log.Information("Making DELETE call to {URI} {response}", _httpClient.BaseAddress + endpoint, typeof(TResponse));
                var response = await _httpClient.DeleteFromJsonAsync<HttpResponseMessage>(endpoint);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TResponse>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error DELETE POST call to {URI} {response}", _httpClient.BaseAddress + endpoint, typeof(TResponse));
                return default;
            }
        }
    }
}
