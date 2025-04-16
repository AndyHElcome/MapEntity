using MMIv8_Ktype.Models.Util;
using MMIv8_Ktype.Models.Collections;
using System.Net.Http.Json;
using System.Web;
using Serilog;

namespace MMIv8_Ktype.Api
{
    public sealed class MMIv8_KtypeService
    {
        private readonly HttpClient _httpClient;

        public MMIv8_KtypeService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<MMIv8_Ktype.Models.Collections.Version?> GetCurrentVersion()
        {
            return await this.GetFromJsonAsync<MMIv8_Ktype.Models.Collections.Version>("version/currentversion");
        }

        private async Task<T?> GetFromJsonAsync<T>(string endPoint)
        {
            try
            {
                Log.Information("Making GetFromJsonAsync call to {URI}", _httpClient.BaseAddress + endPoint);
                var response = await _httpClient.GetFromJsonAsync<T?>(endPoint);

                return response;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Making GetFromJsonAsync call to {URI}", _httpClient.BaseAddress + endPoint);
                throw;
            }

        }
    }

    public abstract class MMIv8_KtypeHttpClient(string EndPoint) // TODO change to typed http client https://youtu.be/g-JGay_lnWI?si=PcTbMzsieV3CUG4G
    {
        public readonly HttpClient HttpClient = new HttpClient();
        private readonly static string Scheme = "https";
        private readonly static string Host = "localhost";
        private readonly static int Port = 44304;
        private readonly string Endpoint = EndPoint;

        public string Query { get => _query; }

        private string _query = string.Empty;

        public Uri URI => new UriBuilder
        {
            Scheme = Scheme,
            Host = Host,
            Port = Port,
            Path = Endpoint,
            Query = Query
        }.Uri;


        public void AddParameter(string name, string value)
        {
            var query = HttpUtility.ParseQueryString(URI.Query);
            query[ name ] = value;

            this._query = query.ToString() ?? throw new NullReferenceException($"Unexpected null value whilst adding parameter to URI Name {name} Value {value}");
        }

        public void AddParameter(KeyValuePair<string, object> parameter)
        {
            this.AddParameter(parameter.Key, parameter.Value.ToString() ?? throw new NullReferenceException($"Unexpected null value whilst converting parameter to string parameter {parameter.ToString()}"));
        }

        public void AddParameter(Dictionary<string, object> parameters)
        {
            foreach (var parameter in parameters)
                this.AddParameter(parameter);
        }

        public abstract Task<HttpContent> MakeCall(Serilog.ILogger Log);
    }

    public class PostCall(string EndPoint) : MMIv8_KtypeHttpClient(EndPoint)
    {
        public HttpContent? Content { get => _content; }
        private HttpContent? _content;

        public void SetJsonContent(object contentObject)
        {
            _content = JsonContent.Create(contentObject);
        }

        public override async Task<HttpContent> MakeCall(Serilog.ILogger Log)
        {
            try
            {
                this.AddParameter("Cache-Control", "no-cache");
                Log.Information("Making POST call to {URI} {Content}", this.URI, Content?.ToString() ?? "");
                var response = await this.HttpClient.PostAsync(this.URI, Content);

                response.EnsureSuccessStatusCode();

                return response.Content;
            }
            catch (Exception ex) 
            {
                Log.Error(ex, "Error making POST call to {URI}", this.URI);
                throw;
            }
        }
    }

    public class GetCall(string EndPoint) : MMIv8_KtypeHttpClient(EndPoint)
    {
        public override async Task<HttpContent> MakeCall(Serilog.ILogger Log)
        {
            try
            {
                this.AddParameter("Cache-Control", "no-cache");
                Log.Information("Making GET call to {URI}", this.URI);
                var response = await this.HttpClient.GetAsync(this.URI);
                response.EnsureSuccessStatusCode();
                return response.Content;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error making GET call to {URI}", this.URI);
                throw;
            }
        }
    }

    public class CreateMakeModelMatch : PostCall
    {
        public CreateMakeModelMatch(ImportMatchMakeModel model) : base("Match/MakeModel/CreateMakeModelMatch")
        {
            this.SetJsonContent(model);
        }
    }

    public class StorePartialMatchBase : PostCall
    {
        public StorePartialMatchBase(MatchBaseType matchBaseType, string matchHash, decimal? newScore = null) : base("Match/Base/StorePartialMatchBase")
        {
            this.AddParameter("MatchBaseType", matchBaseType.ToString());
            this.AddParameter("MatchHash", matchHash);

            if (newScore is not null)
                this.AddParameter("NewScore", newScore.ToString() ?? throw new NullReferenceException($"Unexpected null value whilst converting decimal"));
        }
    }

    public class RemovePartialMatchBase : PostCall
    {
        public RemovePartialMatchBase(MatchBaseType matchBaseType, string matchHash) : base("Match/Base/RemovePartialMatchBase")
        {
            this.AddParameter("MatchBaseType", matchBaseType.ToString());
            this.AddParameter("MatchHash", matchHash);
        }
    }

    public class UpdateMatchBaseScore : PostCall
    {
        public UpdateMatchBaseScore(MatchBaseType matchBaseType, string matchHash, decimal newScore) : base("Match/Base/UpdateMatchBaseScore")
        {
            this.AddParameter("MatchBaseType", matchBaseType.ToString());
            this.AddParameter("MatchHash", matchHash);
            this.AddParameter("NewScore", newScore.ToString() ?? throw new NullReferenceException($"Unexpected null value whilst converting decimal"));
        }
    }

    public class EntityMatch : PostCall
    {
        public EntityMatch(int KtypNr, int MMI_V8_Key) : base("Match/Check/EntityMatch")
        {
            this.AddParameter("KtypNr", KtypNr.ToString());
            this.AddParameter("MMI_V8_Key", MMI_V8_Key.ToString());
        }
    }

    public class CurrentVersion : GetCall
    {
        public CurrentVersion() : base("Version/CurrentVersion")
        {
        }
    }
}
