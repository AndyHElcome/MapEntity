using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api;
using MMIv8_Ktype.Core.Services;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Core.Services.Mapping;
using MongoDB.Bson;

namespace MMIv8_Ktype.Core.Contexts
{
    public class VersionProviderMongo(VersionService versionService, UserService userService) : IVersionProvider
    {
        public ObjectId VersionID { get; } = versionService.GetCurrentVersion().Result?.VersionID
                                          ?? versionService.Create("Legacy", "Legacy", userService.CreateAndReturn("Admin").Result).Result?.VersionID //TODO Test this works
                                          ?? throw new NotImplementedException();

        public StatusChange NewStatus(Status status, string? detail = null) => new()
        {
            Status = status,
            DateOfChange = DateTime.Now,
            VersionID = VersionID,
            Detail = detail
        };
    }
}
