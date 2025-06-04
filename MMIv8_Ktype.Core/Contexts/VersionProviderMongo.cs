using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api;
using MMIv8_Ktype.Core.Services;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Core.Services.Mapping;
using MongoDB.Bson;

namespace MMIv8_Ktype.Core.Contexts
{
    public class VersionProviderMongo : IVersionProvider
    {
        public ObjectId VersionID { get; }

        public VersionProviderMongo(VersionService versionService, UserService userService)
        {
            var versionResult = versionService.GetCurrentVersion().Result;

            if (!versionResult.IsSuccess)
            {
                var userResult = userService.GetByName("Admin").Result;

                if (!userResult.IsSuccess)
                    userResult = userService.Create("Admin").Result;

                if (!userResult.IsSuccess)
                    throw new NotImplementedException();

                versionResult = versionService.Create("Legacy", "Legacy", userResult.Value).Result;
            }

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
}
