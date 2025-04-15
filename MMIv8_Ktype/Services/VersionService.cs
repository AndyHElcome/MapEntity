using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Models.Collections;
using MongoDB.Driver;

namespace MMIv8_Ktype.Core.Services
{
    public class VersionService(MongoDBContext MMIv8_Ktype, 
                                MongoBaseContext BaseContext, 
                                UserService UserService) //TODO remove user service and move calls containing into anouther mapping esq service
    {
        public async Task<IAsyncCursor<Models.Collections.Version>> GetAll()
        {
            return await BaseContext.GetCursor(MMIv8_Ktype.Collections.Version);
        }

        public async Task<Models.Collections.Version?> GetCurrentVersion()
        {
            var filter = Builders<Models.Collections.Version>.Filter.Empty;
            var sort = Builders<Models.Collections.Version>.Sort.Descending(m => m.VersionNumber);

            return await BaseContext.GetSingleDocument(MMIv8_Ktype.Collections.Version, sort: sort);
        }

        public async Task<Models.Collections.Version?> GetByVersion(int version)
        {
            var filter = Builders<Models.Collections.Version>.Filter.Eq(e => e.VersionNumber, version);
            return await BaseContext.GetSingleDocument(MMIv8_Ktype.Collections.Version, filter: filter);
        }

        public async Task<Models.Collections.Version?> Create(CreateBaseVersion model)
        {
            var currentVersion = await GetCurrentVersion();
            int newVersionNumber = currentVersion is null ? 0 : currentVersion.VersionNumber + 1;

            var user = await UserService.GetByName(model.User);

            if (user is null)
            {
                user = new User(model.User);
                await UserService.Create(model.User);
            }

            Models.Collections.Version newVersion = new()
            { 
                VersionNumber = newVersionNumber,
                TecDocEntityVersion = model.TecDocEntityVersion,
                MMIv8EntityVersion = model.MMIv8EntityVersion,
                User = user,
            };

            // save
            await BaseContext.Create(MMIv8_Ktype.Collections.Version, newVersion);

            return await GetCurrentVersion();
        }

        public async Task Delete(int version)
        {
            var filter = Builders<Models.Collections.Version>.Filter.Eq(e => e.VersionNumber, version);
            await BaseContext.Delete(MMIv8_Ktype.Collections.Version, filter);

            await GetCurrentVersion();
        }
    }
}
