using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Core.Services.Source;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Indexes;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MMIv8_Ktype.Core.Endpoints
{
    public class SourceMMIv8Endpoints(SourceMMIv8Service sourceEntityService,
                                      SourceMMIv8UpdateService updateService) : SourceEntityEndpoints<SourceMMIv8>(sourceEntityService, updateService), ISourceMMIv8Endpoints;
    

    public class SourceTecDocPCEndpoints(SourceTecDocPCService sourceEntityService,
                                         SourceTecDocPCUpdateService updateService) : SourceEntityEndpoints<SourceTecDocPC>(sourceEntityService, updateService), ISourceTecDocPCEndpoints;
    

    public class SourceEntityEndpoints<T>(SourceEntityService<T> sourceEntityService,
                                          ISourceEntityUpdateService<T> updateService) : ISourceEntityEndpoints<T>
        where T : SourceEntity
    {
        public async Task<PagedResponse<T>> GetAll(int Page, int PageSize)
        {
            return await sourceEntityService.PaginateDocuments<T>(page: Page, pageSize: PageSize);
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

        public async Task Create(T sourceEntity)
        {
            await sourceEntityService.Create(sourceEntity);
        }

        public async Task Bulkload(T[] sourceEntities)
        {
            await sourceEntityService.Create(sourceEntities);
        }

        public async Task DeleteAll()
        {
            await sourceEntityService.DeleteAll();
        }
    }
}
