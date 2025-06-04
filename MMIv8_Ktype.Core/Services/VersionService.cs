using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Models.Collections;
using Version = MMIv8_Ktype.Models.Collections.Version;
using MongoDB.Driver;
using System;
using Serilog;
using MongoDB.Bson;
using MMIv8_Ktype.Models;

namespace MMIv8_Ktype.Core.Services
{
    public class VersionService(MongoDBContext MMIv8_Ktype) : BaseService<Version, ObjectId>(MMIv8_Ktype.Collections.Version)
    {
        public async Task<Result<Version>> GetCurrentVersion()
        {
            var sort = Builders<Version>.Sort.Descending(m => m.VersionNumber);

            var version = await base.GetFindFluent(sort: sort).FirstOrDefaultAsync();

            return version is not null ? version : Error.NotFound("Version.NoCurrentVersion", "No current version exist");
        }

        public async Task<Result<Version>> GetByVersion(int versionNumber)
        {
            var filter = Builders<Version>.Filter.Eq(e => e.VersionNumber, versionNumber);

            var version = await base.GetFindFluent(filter: filter).FirstOrDefaultAsync();

            return version is not null ? version : Error.NotFound("Version.NotFoundByVersionNumber", $"No version exists with VersionNumber {versionNumber}");
        }

        public async Task<Result<List<Version>>> GetByUser(string userName)
        {
            var filter = Builders<Version>.Filter.Eq(e => e.User.Name, userName);

            var version = await base.GetFindFluent(filter: filter).ToListAsync();

            return version is { Count: > 0} ? version : Error.NoContent("Version.NotFoundByUser", $"No versions exist with User {userName}");
        }

        public async Task<Result<Version>> Create(string tecdocEntityVersion, string mmiv8EntityVersion, User user)
        {
            var currentVersionResult = await GetCurrentVersion();
            int newVersionNumber = currentVersionResult.IsSuccess ? currentVersionResult.Value.VersionNumber + 1 : 0 ;

            var version = new Version(newVersionNumber, tecdocEntityVersion, mmiv8EntityVersion, user);

            var createVersionResult = await base.Create(version);

            if (!createVersionResult.IsSuccess)
                return createVersionResult.Error!;

            return await GetCurrentVersion();
        }
    }
}
