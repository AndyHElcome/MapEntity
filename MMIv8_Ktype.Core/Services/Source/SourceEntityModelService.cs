using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MongoDB.Driver;

namespace MMIv8_Ktype.Core.Services.Source
{
    public class SourceTecDocEntityModelService(MongoDBContext MMIv8_Ktype) : SourceEntityModelService(MMIv8_Ktype.Collections.SourceTecDocPCModel);

    public class SourceMMIv8EntityModelService(MongoDBContext MMIv8_Ktype) : SourceEntityModelService(MMIv8_Ktype.Collections.SourceMMIv8Model);

    public abstract class SourceEntityModelService(IMongoCollection<MongoSourceEntityModel> Collection) : BaseService<MongoSourceEntityModel, string>(Collection)
    {
        public async Task<Result<List<MongoSourceEntityModel>>> GetByNotId(string[] documentId)
        {
            var filter = Builders<MongoSourceEntityModel>.Filter.Nin(e => e.DocumentId, documentId);
            var result = await base.GetFindFluent(filter: filter).ToListAsync();

            return result is not null ? result : Error.NoContent("SourceEntityModel.NoContentByNotId", $"Could not find any {base.Collection.CollectionNamespace}");
        }

        public async Task<Result<MongoSourceEntityModel>> GetByMakeModel(string make, string model)
        {
            if (make == string.Empty || model == string.Empty)
                return Error.Validation("SourceEntityModel.GetByMakeModelValidation", "Make and Model must not be empty");

            var filterBuilder = Builders<MongoSourceEntityModel>.Filter;
            var filter = filterBuilder.Eq(e => e.Make, make)
                       & filterBuilder.Eq(e => e.Model, model);
            var result = await base.GetFindFluent(filter: filter).FirstOrDefaultAsync();

            return result is not null ? result : Error.NotFound("SourceEntityModel.NotFoundByMakeModel", $"No SourceEntityModel exists for {base.Collection.CollectionNamespace} {make} {model}");
        }
    }
}
