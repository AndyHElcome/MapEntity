using Microsoft.AspNetCore.Mvc.RazorPages;
using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Core.Services;
using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Core.Services.Source;
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
                                         MappingService mappingService) : IEntityRelationEndpoints
    {
        public async Task<PagedResponse<EntityRelation>> GetAll(int Page = 1, int PageSize = 100)
        {
            return await entityRelationService.PaginateDocuments<EntityRelation>(page: Page, pageSize: PageSize);
        }

        public async Task<PagedResponse<EntityRelation>> GetByVersion(ObjectId versionID, int Page = 1, int PageSize = 100)
        {
            var filter = Builders<EntityRelation>.Filter.Eq(e => e.VersionID, versionID);

            return await entityRelationService.PaginateDocuments<EntityRelation>(filter: filter, page: Page, pageSize: PageSize);
        }

        public async Task<PagedResponse<EntityRelation>> GetCurrentEntityRelations(int Page = 1, int PageSize = 100) //TODO change to stream call
        {
            var version = await versionService.GetCurrentVersion();
            return await this.GetByVersion(version.DocumentId, Page, PageSize);
        }

        public async Task<PagedResponse<EntityRelation>> GetPreviousEntityRelations(int Page = 1, int PageSize = 100) //TODO change to stream call
        {
            var version = await mappingService.GetPreviousVersionIDWithEntityRelations();
            return await this.GetByVersion(version.DocumentId, Page, PageSize);
        }

        public async Task CreateEntityRelation(int versionNumber, List<PutEntityRelationRequest> entityRelationsRequest)
        {
            await mappingService.CreateEntityRelation(versionNumber, entityRelationsRequest);
        }

        public async Task DeleteAll()
        {
            await entityRelationService.DeleteAll();
        }
    }
}
