using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Models.Collections;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace MMIv8_Ktype.Core.Services.Source
{
    public class SourceTecDocPCService(MongoDBContext MMIv8_Ktype, 
                                       MongoBaseContext BaseContext) : ISourceEntityService<MongoSourceTecDocPC>, IMongoCollectionService<MongoSourceTecDocPC>
    {
        public IMongoCollection<MongoSourceTecDocPC> Collection => MMIv8_Ktype.Collections.SourceTecDocPC;

        public async Task<IAsyncCursor<MongoSourceTecDocPC>> GetAll(int? batchSize = null)
        {
            return await BaseContext.GetCursor(Collection, batchSize: batchSize);
        }

        public async Task<MongoSourceTecDocPC?> GetById(ObjectId SourceEntityID)
        {
            var filter = Builders<MongoSourceTecDocPC>.Filter.Eq(e => e.SourceEntityID, SourceEntityID);
            var result = await BaseContext.GetSingleDocument(Collection, filter: filter);

            if (result is null)
                Log.Warning("No TecDocEntity found with SourceEntityID {SourceEntityID}", SourceEntityID);

            return result;
        }

        public async Task<MongoSourceTecDocPC?> GetByExternalId(int KTypNr)
        {
            var filter = Builders<MongoSourceTecDocPC>.Filter.Eq(e => e.KTypNr, KTypNr);
            var result = await BaseContext.GetSingleDocument(Collection, filter: filter);

            if (result is null)
                Log.Warning("No TecDocEntity found with KTypNr {KTypNr}", KTypNr);

            return result;
        }

        public async Task<IAsyncCursor<MongoSourceTecDocPC>> GetByModelId(string SourceEntityModelHash, int? batchSize = null)
        {
            var filter = Builders<MongoSourceTecDocPC>.Filter.Eq(e => e.SourceEntityModelHash, SourceEntityModelHash);
            return await BaseContext.GetCursor(Collection, filter: filter, batchSize: batchSize);
        }

        public CombinationPipeline<MongoSourceTecDocPC> UpdateSourceEntity(MongoSourceTecDocPC entity)
        {
            var filter = Builders<MongoSourceTecDocPC>.Filter.Eq(c => c.SourceEntityID, entity.SourceEntityID);

            return new CombinationPipeline<MongoSourceTecDocPC>(Collection, filter);
        }

        public async Task Create(MongoSourceTecDocPC model, bool validate = true)
        {
            if (validate)
            {
                var filter = Builders<MongoSourceTecDocPC>.Filter.Eq(e => e.KTypNr, model.KTypNr);
                if (await BaseContext.GetSingleDocument(Collection, filter) is not null)
                    throw new Exception($"Entity '" + model.KTypNr + "' already exists");
            }

            await BaseContext.Create(Collection, model);
        }

        public async Task CreateBulk(List<MongoSourceTecDocPC> model)
        {
            await BaseContext.Create(Collection, model.ToArray());
        }

        public async Task<DeleteResult> DeleteAll(FilterDefinition<MongoSourceTecDocPC>? filter = null)
        {
            filter ??= Builders<MongoSourceTecDocPC>.Filter.Empty;
            return await BaseContext.Delete(Collection, filter);
        }

        public async Task<DeleteResult> Delete(ObjectId id)
        {
            var filter = Builders<MongoSourceTecDocPC>.Filter.Eq(e => e.SourceEntityID, id);
            return await BaseContext.Delete(Collection, filter);
        }

        public async Task<DeleteResult> DeleteByExternalId(int KTypNr)
        {
            var filter = Builders<MongoSourceTecDocPC>.Filter.Eq(e => e.KTypNr, KTypNr);
            return await BaseContext.Delete(Collection, filter);
        }
    }
}
