using Microsoft.AspNetCore.Mvc.RazorPages;
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
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace MMIv8_Ktype.Core.Endpoints
{
    public class EntityRelationEndpoints(EntityRelationService entityRelationService,
                                         VersionService versionService,
                                         MappingService mappingService) : BaseEndpoints<EntityRelation, ObjectId, FilterQuery<EntityRelation>, SortQuery<EntityRelation>>(entityRelationService), IEntityRelationEndpoints
    {

        //TODO make filterquery
        public async Task<SerializableResult<PagedCursorResponse<EntityRelation>>> GetByVersion(ObjectId versionID, [AsParameters] PagedCursorRequest<ObjectId> pagedRequest)
        {
            var filter = Builders<EntityRelation>.Filter.Eq(e => e.VersionID, versionID);

            return await entityRelationService.PaginateDocumentsByCursor<EntityRelation, ObjectId>(filter: filter, cursor: pagedRequest.GetCursor(), pageSize: pagedRequest.PageSize ?? 0);
        }

        public async Task<SerializableResult<PagedCursorResponse<EntityRelation>>> GetCurrentEntityRelations([AsParameters] PagedCursorRequest<ObjectId> pagedRequest) //TODO change to stream call
        {
            var versionResult = await versionService.GetCurrentVersion();
            if (!versionResult.IsSuccess)
                return Result.Failure<PagedCursorResponse<EntityRelation>>(versionResult.Error!);

            return await this.GetByVersion(versionResult.Value.DocumentId, pagedRequest);
        }

        public async Task<SerializableResult<PagedCursorResponse<EntityRelation>>> GetPreviousEntityRelations([AsParameters] PagedCursorRequest<ObjectId> pagedRequest) //TODO change to stream call
        {
            var versionResult = await mappingService.GetPreviousVersionIDWithEntityRelations();
            if (!versionResult.IsSuccess)
                return Result.Failure<PagedCursorResponse<EntityRelation>>(versionResult.Error!);

            return await this.GetByVersion(versionResult.Value.DocumentId, pagedRequest);
        }

        public async Task<Result> CreateEntityRelation(int versionNumber, List<PutEntityRelationRequest> entityRelationsRequest)
        {
            return await mappingService.CreateEntityRelation(versionNumber, entityRelationsRequest);
        }

        public async Task<Result> DeleteAll()
        {
            return await entityRelationService.DeleteAll();
        }
    }
}
