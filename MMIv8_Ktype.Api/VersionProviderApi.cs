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
        var versionEndpoints = RefitClient.CreateService<IVersionEndpoints>();

        var versionResult = versionEndpoints.GetCurrentVersion().Result;

        if (!versionResult.IsSuccess)
        {
            var userEndpoints = RefitClient.CreateService<IUserEndpoints>();

            var userResult = userEndpoints.GetByUserName("Admin").Result;

            if (!userResult.IsSuccess)
                userResult = userEndpoints.Create("Admin").Result;

            if (!userResult.IsSuccess)
                throw new NotImplementedException();

            versionResult = versionEndpoints.CreateVersion("Legacy", "Legacy", userResult.ToResult().Value.Name).Result;
        }

        if (!versionResult.IsSuccess)
            throw new NotImplementedException();

        VersionID = versionResult.ToResult().Value.DocumentId;
    }

    public StatusChange NewStatus(Status status, string? detail = null) => new()
    {
        Status = status,
        DateOfChange = DateTime.Now,
        VersionID = VersionID,
        Detail = detail
    };
}



