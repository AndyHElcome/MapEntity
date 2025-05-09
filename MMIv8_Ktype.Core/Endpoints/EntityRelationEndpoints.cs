using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Core.Services;
using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Indexes;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace MMIv8_Ktype.Core.Endpoints
{
    public class EntityRelationEndpoints(EntityRelationService entityRelationService,
                                         VersionService versionService,
                                         MappingService mappingService) : IEntityRelationEndpoints
    {
        public async Task<List<EntityRelation>> GetAll() //TODO change to stream call
        {
            return await entityRelationService.GetMultipleDocuments();
        }

        public async Task<List<EntityRelation>> GetByVersion(ObjectId versionID)
        {
            var filter = Builders<EntityRelation>.Filter.Eq(e => e.VersionID, versionID);

            return await entityRelationService.GetMultipleDocuments(filter: filter);
        }

        public async Task<List<EntityRelation>> GetCurrentEntityRelations() //TODO change to stream call
        {
            var version = await versionService.GetCurrentVersion();
            return await this.GetByVersion(version.DocumentId);
        }

        public async Task<List<EntityRelation>> GetPreviousEntityRelations() //TODO change to stream call
        {
            var version = await mappingService.GetPreviousVersionIDWithEntityRelations();
            return await this.GetByVersion(version.DocumentId);
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
