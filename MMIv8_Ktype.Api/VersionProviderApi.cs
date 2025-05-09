using Serilog;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Models.Collections;
using MongoDB.Bson;

namespace MMIv8_Ktype.Api;

public class VersionProviderApi(RefitClient RefitClient) : IVersionProvider
{
    public ObjectId VersionID { get; } = RefitClient.CreateService<IVersionEndpoints>().GetCurrentVersionID().Result
                                      ?? RefitClient.CreateService<IVersionEndpoints>().CreateVersion(new("Legacy", "Legacy", "Admin")).Result?.DocumentId
                                      ?? throw new NotImplementedException(); //TODO Test this works

    public StatusChange NewStatus(Status status, string? detail = null) => new()
    {
        Status = status,
        DateOfChange = DateTime.Now,
        VersionID = VersionID,
        Detail = detail
    };
}



