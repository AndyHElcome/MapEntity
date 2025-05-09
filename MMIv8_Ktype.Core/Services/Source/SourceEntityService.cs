using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Indexes;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace MMIv8_Ktype.Core.Services.Source
{
    public class SourceMMIv8Service(MongoDBContext MMIv8_Ktype, IVersionProvider versionProvider) : SourceEntityService<MongoSourceMMIv8>(MMIv8_Ktype.Collections.SourceMMIv8, versionProvider);

    public class SourceTecDocPCService(MongoDBContext MMIv8_Ktype, IVersionProvider versionProvider) : SourceEntityService<MongoSourceTecDocPC>(MMIv8_Ktype.Collections.SourceTecDocPC, versionProvider);

    public abstract class SourceEntityService<T>(IMongoCollection<T> Collection, IVersionProvider versionProvider) : BaseServiceWithDifferences<T, ObjectId>(Collection, versionProvider)
        where T : SourceEntity
    {
        public async Task<T?> GetByExternalId(int externalId)
        {
            var filter = Builders<T>.Filter.Eq(e => e.ExternalId, externalId);
            var result = await base.GetSingleDocument(filter);

            if (result is null)
                Log.Warning("No {type} found with ExternalId {ExternalId}", typeof(T), externalId);

            return result;
        }

        public async Task<IAsyncCursor<T>> GetByModelId(string SourceEntityModelHash, int? batchSize = null)
        {
            var filter = Builders<T>.Filter.Eq(e => e.SourceEntityModelHash, SourceEntityModelHash);
            return await base.GetCursor(filter: filter, batchSize: batchSize);
        }
    }
}
