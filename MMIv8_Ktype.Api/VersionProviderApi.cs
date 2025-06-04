using Serilog;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Models.Collections;
using MongoDB.Bson;

namespace MMIv8_Ktype.Api;

public class VersionProviderApi : IVersionProvider
{
    public ObjectId VersionID { get; } //TODO Test this works

    public VersionProviderApi(RefitClient RefitClient)
    {
        var versionResult = RefitClient.CreateService<IVersionEndpoints>().GetCurrentVersion().Result;

        if (!versionResult.IsSuccess)
            versionResult = RefitClient.CreateService<IVersionEndpoints>().CreateVersion(new("Legacy", "Legacy", "Admin")).Result;

        if (!versionResult.IsSuccess)
            throw new NotImplementedException();

        VersionID = versionResult.Value.DocumentId;
    }

    public StatusChange NewStatus(Status status, string? detail = null) => new()
    {
        Status = status,
        DateOfChange = DateTime.Now,
        VersionID = VersionID,
        Detail = detail
    };
}



