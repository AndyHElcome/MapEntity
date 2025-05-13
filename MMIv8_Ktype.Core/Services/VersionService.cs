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
        public async Task<Version> GetCurrentVersion()
        {
            var sort = Builders<Version>.Sort.Descending(m => m.VersionNumber);

            return await base.GetFindFluent(sort: sort).FirstOrDefaultAsync();
        }

        public async Task<ObjectId> GetCurrentVersionID()
        {
            var sort = Builders<Version>.Sort.Descending(m => m.VersionNumber);
            var projection = Builders<Version>.Projection.Expression(c => c.DocumentId);

            return await base.GetFindFluent(sort: sort).Project(projection).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Get older versions skip = 0 is current version.
        /// </summary>
        /// <param name="skip"></param>
        /// <returns></returns>
        public async Task<ObjectId> GetPreviousVersionID(int skip = 1)
        {
            var sort = Builders<Version>.Sort.Descending(m => m.VersionNumber);
            var projection = Builders<Version>.Projection.Expression(c => c.DocumentId);

            return await base.GetFindFluent(sort: sort, skip: skip).Project(projection).FirstOrDefaultAsync();
        }

        public async Task<Version> GetByVersion(int version)
        {
            var filter = Builders<Version>.Filter.Eq(e => e.VersionNumber, version);

            return await base.GetFindFluent(filter: filter).FirstOrDefaultAsync();
        }

        public async Task<List<Version>> GetByUser(string userName)
        {
            var filter = Builders<Version>.Filter.Eq(e => e.User.Name, userName);

            return await base.GetFindFluent(filter: filter).ToListAsync();
        }

        public async Task<Version> Create(string tecdocEntityVersion, string mmiv8EntityVersion, User user)
        {
            var currentVersion = await GetCurrentVersion();
            int newVersionNumber = currentVersion is null ? 0 : currentVersion.VersionNumber + 1;

            await base.Create( new Version(newVersionNumber, tecdocEntityVersion, mmiv8EntityVersion, user) );

            return await GetCurrentVersion();
        }

        public async Task<Version?> Update(int versionNumber, string? tecdocEntityVersion = null, string? mmiv8EntityVersion = null, User? user = null)
        {
            if (tecdocEntityVersion is null && mmiv8EntityVersion is null && user is null)
            {
                Log.Warning("No updates given for Version {versionNumber}", versionNumber);
                return null; 
            }

            var filter = Builders<Version>.Filter.Eq(c => c.VersionNumber, versionNumber);
            var update = Builders<Version>.Update.Combine();

            if (tecdocEntityVersion is not null)
                update = update.Set(c => c.TecDocEntityVersion, tecdocEntityVersion);

            if (mmiv8EntityVersion is not null)
                update = update.Set(c => c.MMIv8EntityVersion, mmiv8EntityVersion);

            if (user is not null)
                update = update.Set(c => c.User, user);

            return await base.FindOneAndUpdate(filter, update);
        }
    }
}
