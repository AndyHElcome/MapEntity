using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Models.Collections;
using MongoDB.Driver;

namespace MMIv8_Ktype.Core.Services.Source
{
    public class SourceTecDocEntityModelService(MongoDBContext MMIv8_Ktype) : SourceEntityModelService(MMIv8_Ktype.Collections.SourceTecDocPCModel);

    public class SourceMMIv8EntityModelService(MongoDBContext MMIv8_Ktype) : SourceEntityModelService(MMIv8_Ktype.Collections.SourceMMIv8Model);

    public abstract class SourceEntityModelService(IMongoCollection<MongoSourceEntityModel> Collection) : BaseService<MongoSourceEntityModel, string>(Collection)
    {
        public async Task<IAsyncCursor<MongoSourceEntityModel>> GetByNotId(string[] id, int? batchSize = null)
        {
            var filter = Builders<MongoSourceEntityModel>.Filter.Nin(e => e.SourceEntityModelHash, id);
            return await base.GetAll(filter: filter, batchSize: batchSize);
        }

        public async Task<MongoSourceEntityModel?> GetByMakeModel(string make, string model)
        {
            if (make == string.Empty || model == string.Empty)
                return null;

            var filterBuilder = Builders<MongoSourceEntityModel>.Filter;
            var filter = filterBuilder.Eq(e => e.Make, make)
                       & filterBuilder.Eq(e => e.Model, model);

            return await base.GetSingleDocument(filter: filter);
        }
    }
}
