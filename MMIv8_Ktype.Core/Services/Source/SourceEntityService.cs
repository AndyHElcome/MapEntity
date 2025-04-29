using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Indexes;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace MMIv8_Ktype.Core.Services.Source
{
    public class SourceMMIv8Service(MongoDBContext MMIv8_Ktype,
                                    MongoBaseContext BaseContext) : SourceEntityService<MongoSourceMMIv8>(BaseContext)
    {
        public override IMongoCollection<MongoSourceMMIv8> Collection => MMIv8_Ktype.Collections.SourceMMIv8;
    }

    public class SourceTecDocPCService(MongoDBContext MMIv8_Ktype,
                                      MongoBaseContext BaseContext) : SourceEntityService<MongoSourceTecDocPC>(BaseContext)
    {
        public override IMongoCollection<MongoSourceTecDocPC> Collection => MMIv8_Ktype.Collections.SourceTecDocPC;
    }

    public abstract class SourceEntityService<T>(MongoBaseContext BaseContext) : IMongoCollectionService<T>
        where T : SourceEntity
    {
        public abstract IMongoCollection<T> Collection { get; }

        public async Task<IAsyncCursor<T>> GetAll(int? batchSize)
        {
            return await BaseContext.GetCursor(Collection, batchSize: batchSize);
        }

        public async Task<T?> GetById(ObjectId SourceEntityID)
        {
            var filter = Builders<T>.Filter.Eq(e => e.SourceEntityID, SourceEntityID);
            var result = await BaseContext.GetSingleDocument(Collection, filter);

            if (result is null)
                Log.Warning("No MMIv8Entity found with SourceEntityID {SourceEntityID}", SourceEntityID);

            return result;
        }

        public async Task<T?> GetByExternalId(int externalId)
        {
            var filter = Builders<T>.Filter.Eq(e => e.ExternalId, externalId);
            var result = await BaseContext.GetSingleDocument(Collection, filter);

            if (result is null)
                Log.Warning("No {type} found with ExternalId {ExternalId}", typeof(T), externalId);

            return result;
        }

        public async Task<IAsyncCursor<T>> GetByModelId(string SourceEntityModelHash, int? batchSize = null)
        {
            var filter = Builders<T>.Filter.Eq(e => e.SourceEntityModelHash, SourceEntityModelHash);
            return await BaseContext.GetCursor(Collection, filter: filter, batchSize: batchSize);
        }

        public CombinationPipeline<T> UpdateSourceEntity(T entity)
        {
            var filter = Builders<T>.Filter.Eq(c => c.SourceEntityID, entity.SourceEntityID);

            return new CombinationPipeline<T>(Collection, filter);
        }

        public async Task Create(T model, bool validate = true)
        {
            if (validate)
            {
                var filter = Builders<T>.Filter.Eq(e => e.ExternalId, model.ExternalId);
                if (await BaseContext.GetSingleDocument(Collection, filter) is not null)
                    throw new Exception($"Entity '" + model.ExternalId + "' already exists");
            }

            await BaseContext.Create(Collection, (T)model);
        }

        public async Task CreateBulk(List<T> model)
        {
            await BaseContext.Create(Collection, model.ToArray());
        }

        public async Task<DeleteResult> DeleteAll(FilterDefinition<T>? filter = null)
        {
            filter ??= Builders<T>.Filter.Empty;
            return await BaseContext.Delete(Collection, filter);
        }

        public async Task<DeleteResult> Delete(ObjectId id)
        {
            var filter = Builders<T>.Filter.Eq(e => e.SourceEntityID, id);
            return await BaseContext.Delete(Collection, filter);
        }

        public async Task<DeleteResult> DeleteByExternalId(int externalId)
        {
            var filter = Builders<T>.Filter.Eq(e => e.ExternalId, externalId);
            return await BaseContext.Delete(Collection, filter);
        }
    }
}
