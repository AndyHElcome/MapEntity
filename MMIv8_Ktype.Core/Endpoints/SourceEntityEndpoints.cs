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
using System.Reflection.Metadata;

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
        public async Task<SerializableResult<PagedCursorResponse<T>>> GetAll(string? Cursor, int PageSize)
        {
            var objectId = ObjectId.TryParse(Cursor, out var objectid) ? objectid : ObjectId.Empty;
            return await sourceEntityService.PaginateDocumentsByCursor<T, ObjectId>(cursor: objectId, pageSize: PageSize);
        }

        public async Task<SerializableResult<T>> GetById(ObjectId EntityId)
        {
            return await sourceEntityService.GetById(EntityId);
        }

        public async Task<SerializableResult<T>> GetByExternalId(int ExternalId)
        {
            return await sourceEntityService.GetByExternalId(ExternalId);
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
