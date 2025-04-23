using Serilog;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Api.Endpoints;

namespace MMIv8_Ktype.Api;

    public class VersionProviderApi(ILogger log) : IVersionProvider
    {
        public RefitClient RefitClient { get; } = new RefitClient(log);
        public Models.Collections.Version Version => RefitClient.CreateService<IVersionEndpoints>().GetCurrentVersion().Result ?? new();

        public StatusChange NewStatus(Status status, string? detail = null) => new()
        {
            Status = status,
            DateOfChange = DateTime.Now,
            Version = Version,
            Detail = detail
        };
    }

