using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Models.Collections;
using MongoDB.Driver;

namespace MMIv8_Ktype.Core.Services.Source
{
    public class SourceTecDocEntityModelService(MongoDBContext MMIv8_Ktype,
                                                MongoBaseContext BaseContext) : SourceEntityModelService(BaseContext)
    {
        public override IMongoCollection<MongoSourceEntityModel> Collection => MMIv8_Ktype.Collections.SourceTecDocPCModel;
    }

    public class SourceMMIv8EntityModelService(MongoDBContext MMIv8_Ktype,
                                               MongoBaseContext BaseContext) : SourceEntityModelService(BaseContext)
    {
        public override IMongoCollection<MongoSourceEntityModel> Collection => MMIv8_Ktype.Collections.SourceMMIv8Model;
    }

    public abstract class SourceEntityModelService(MongoBaseContext BaseContext) : IMongoCollectionService<MongoSourceEntityModel>
    {
        public abstract IMongoCollection<MongoSourceEntityModel> Collection { get; }

        public async Task<IAsyncCursor<MongoSourceEntityModel>> GetAll(int? batchSize = null)
        {
            return await BaseContext.GetCursor(Collection, batchSize: batchSize);
        }

        public async Task<MongoSourceEntityModel?> GetById(string id)
        {
            var filter = Builders<MongoSourceEntityModel>.Filter.Eq(e => e.SourceEntityModelHash, id);
            return await BaseContext.GetSingleDocument(Collection, filter: filter);
        }

        public async Task<IAsyncCursor<MongoSourceEntityModel>> GetByNotId(string[] id, int? batchSize = null)
        {
            var filter = Builders<MongoSourceEntityModel>.Filter.Nin(e => e.SourceEntityModelHash, id);
            return await BaseContext.GetCursor(Collection, filter: filter, batchSize: batchSize);
        }

        public async Task<MongoSourceEntityModel?> GetByMakeModel(string make, string model)
        {
            if (make == string.Empty || model == string.Empty)
                return null;

            var filterBuilder = Builders<MongoSourceEntityModel>.Filter;
            var filter = filterBuilder.Eq(e => e.Make, make)
                       & filterBuilder.Eq(e => e.Model, model);

            return await BaseContext.GetSingleDocument(Collection, filter: filter);
        }
    }
}
