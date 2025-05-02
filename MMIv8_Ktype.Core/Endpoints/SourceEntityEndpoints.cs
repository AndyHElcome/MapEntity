using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Source;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Indexes;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MMIv8_Ktype.Core.Endpoints
{
    public class SourceMMIv8Endpoints(SourceMMIv8Service sourceEntityService,
                                      SourceMMIv8UpdateService updateService) : SourceEntityEndpoints<MongoSourceMMIv8>(sourceEntityService, updateService), ISourceMMIv8Endpoints
    { }

    public class SourceTecDocPCEndpoints(SourceTecDocPCService sourceEntityService,
                                         SourceTecDocPCUpdateService updateService) : SourceEntityEndpoints<MongoSourceTecDocPC>(sourceEntityService, updateService), ISourceTecDocPCEndpoints
    { }

    public class SourceEntityEndpoints<T>(SourceEntityService<T> sourceEntityService,
                                          ISourceEntityUpdateService<T> updateService) : ISourceEntityEndpoints<T>
        where T : SourceEntity
    {
        public async Task<List<T>> GetAll() //TODO change to stream call
        {
            var response = await sourceEntityService.GetAll(batchSize: 100);
            return await response.ToListAsync();
        }

        public async Task<T?> GetById(ObjectId EntityId)
        {
            return await sourceEntityService.GetById(EntityId);
        }

        public async Task<T?> GetByExternalId(int ExternalId)
        {
            return await sourceEntityService.GetByExternalId(ExternalId);
        }

        public async Task UpdateEntity(T sourceEntity)
        {
            await updateService.UpdateEntity(sourceEntity);
        }
    }
}
