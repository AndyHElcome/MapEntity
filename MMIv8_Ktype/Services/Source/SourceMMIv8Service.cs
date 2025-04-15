using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Models.Collections;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace MMIv8_Ktype.Core.Services.Source
{
    public class SourceMMIv8Service(MongoDBContext MMIv8_Ktype, 
                                    MongoBaseContext BaseContext) : ISourceEntityService<MongoSourceMMIv8>, IMongoCollectionService<MongoSourceMMIv8>
    {
        public IMongoCollection<MongoSourceMMIv8> Collection => MMIv8_Ktype.Collections.SourceMMIv8;

        public async Task<IAsyncCursor<MongoSourceMMIv8>> GetAll(int? batchSize = null)
        {
            return await BaseContext.GetCursor(Collection, batchSize: batchSize);
        }

        public async Task<MongoSourceMMIv8?> GetById(ObjectId SourceEntityID)
        {
            var filter = Builders<MongoSourceMMIv8>.Filter.Eq(e => e.SourceEntityID, SourceEntityID);
            var result = await BaseContext.GetSingleDocument(Collection, filter);

            if (result is null)
                Log.Warning("No MMIv8Entity found with SourceEntityID {SourceEntityID}", SourceEntityID);

            return result;
        }

        public async Task<MongoSourceMMIv8?> GetByExternalId(int MMI_V8_Key)
        {
            var filter = Builders<MongoSourceMMIv8>.Filter.Eq(e => e.MMI_V8_Key, MMI_V8_Key);
            var result = await BaseContext.GetSingleDocument(Collection, filter);

            if (result is null)
                Log.Warning("No MMIv8Entity found with MMI_V8_Key {MMI_V8_Key}", MMI_V8_Key);

            return result;
        }

        public async Task<IAsyncCursor<MongoSourceMMIv8>> GetByModelId(string SourceEntityModelHash, int? batchSize = null)
        {
            var filter = Builders<MongoSourceMMIv8>.Filter.Eq(e => e.SourceEntityModelHash, SourceEntityModelHash);
            return await BaseContext.GetCursor(Collection, filter: filter, batchSize: batchSize);
        }

        public CombinationPipeline<MongoSourceMMIv8> UpdateSourceEntity(MongoSourceMMIv8 entity)
        {
            var filter = Builders<MongoSourceMMIv8>.Filter.Eq(c => c.SourceEntityID, entity.SourceEntityID);

            return new CombinationPipeline<MongoSourceMMIv8>(Collection, filter);
        }

        public async Task Create(MongoSourceMMIv8 model, bool validate = true)
        {
            if (validate)
            {
                var filter = Builders<MongoSourceMMIv8>.Filter.Eq(e => e.MMI_V8_Key, model.MMI_V8_Key);
                if (await BaseContext.GetSingleDocument(Collection, filter) is not null)
                    throw new Exception($"Entity '" + model.MMI_V8_Key + "' already exists");
            }

            await BaseContext.Create(Collection, model);
        }

        public async Task CreateBulk(List<MongoSourceMMIv8> model)
        {
            await BaseContext.Create(Collection, model.ToArray());
        }

        public async Task<DeleteResult> DeleteAll(FilterDefinition<MongoSourceMMIv8>? filter = null)
        {
            filter ??= Builders<MongoSourceMMIv8>.Filter.Empty;
            return await BaseContext.Delete(Collection, filter);
        }

        public async Task<DeleteResult> Delete(ObjectId id)
        {
            var filter = Builders<MongoSourceMMIv8>.Filter.Eq(e => e.SourceEntityID, id);
            return await BaseContext.Delete(Collection, filter);
        }

        public async Task<DeleteResult> DeleteByExternalId(int MMI_V8_Key)
        {
            var filter = Builders<MongoSourceMMIv8>.Filter.Eq(e => e.MMI_V8_Key, MMI_V8_Key);
            return await BaseContext.Delete(Collection, filter);
        }
    }
}
