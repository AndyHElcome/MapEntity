using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Core.Services;
using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Core.Services.Source;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Indexes;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Reflection.Metadata;

namespace MMIv8_Ktype.Core.Endpoints
{
    public class SourceMMIv8Endpoints(SourceMMIv8Service sourceEntityService,
                                      SourceMMIv8EntityModelService sourceEntityModelService,
                                      SourceMMIv8UpdateService updateService) : SourceEntityEndpoints<SourceMMIv8>(sourceEntityService, sourceEntityModelService, updateService), ISourceMMIv8Endpoints;
    

    public class SourceTecDocPCEndpoints(SourceTecDocPCService sourceEntityService,
                                         SourceTecDocEntityModelService sourceEntityModelService,
                                         SourceTecDocPCUpdateService updateService) : SourceEntityEndpoints<SourceTecDocPC>(sourceEntityService, sourceEntityModelService, updateService), ISourceTecDocPCEndpoints;
    

    public class SourceEntityEndpoints<T>(SourceEntityService<T> sourceEntityService, SourceEntityModelService sourceEntityModelService, ISourceEntityUpdateService<T> updateService) : BaseEndpoints<T, ObjectId, FilterQuery<T>, SortQuery<T>>(sourceEntityService), ISourceEntityEndpoints<T>
        where T : SourceEntity
    {
        public async Task<SerializableResult<T>> GetByExternalId(int ExternalId)
        {
            return await sourceEntityService.GetByExternalId(ExternalId);
        }

        public async Task<SerializableResult<List<string>>> GetMakes([FromQuery] string[]? makes, [FromQuery] string[]? models)
        {
            var filterBuilder = Builders<MongoSourceEntityModel>.Filter;
            var filter = filterBuilder.Empty;

            if (makes is not null && makes.Length > 0)
                filter &= filterBuilder.In(e => e.Make, makes);

            if (models is not null && models.Length > 0)
                filter &= filterBuilder.In(e => e.Model, models);

            var result = await sourceEntityModelService.GetDistinctDocuments<string>(nameof(MongoSourceEntityModel.Make), filter: filter);
            result.Value.Sort();

            return result is not null ? result : Error.NotFound("SourceEntityModel.NotFoundByMakeModel", $"No Makes exists for {sourceEntityModelService.Collection.CollectionNamespace} {makes} {models}");
        }

        public async Task<SerializableResult<List<string>>> GetModels([FromQuery] string[]? makes, [FromQuery] string[]? models)
        {
            var filterBuilder = Builders<MongoSourceEntityModel>.Filter;
            var filter = filterBuilder.Empty;

            if (makes is not null && makes.Length > 0)
                filter &= filterBuilder.In(e => e.Make, makes);

            if (models is not null && models.Length > 0)
                filter &= filterBuilder.In(e => e.Model, models);

            var result = await sourceEntityModelService.GetDistinctDocuments<string>(nameof(MongoSourceEntityModel.Model), filter: filter);
            result.Value.Sort();

            return result is not null ? result : Error.NotFound("SourceEntityModel.NotFoundByMakeModel", $"No Models exists for {sourceEntityModelService.Collection.CollectionNamespace} {makes} {models}");
        }

        public async Task<SerializableResult<MongoSourceEntityModel>> GetByMakeModel([FromQuery] string make, [FromQuery] string model)
        {
            return await sourceEntityModelService.GetByMakeModel(make, model);
        }

        public async Task<Result> UpdateEntity(T sourceEntity)
        {
            return await updateService.UpdateEntity(sourceEntity);
        }

        public async Task<Result> Create(T sourceEntity)
        {
            return await sourceEntityService.Create(sourceEntity);
        }

        public async Task<Result> Bulkload(T[] sourceEntities)
        {
            return await sourceEntityService.Create(sourceEntities);
        }

        public async Task<Result> DeleteAll()
        {
            return await sourceEntityService.DeleteAll();
        }
    }
}
