using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Core.Services;
using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Models.Collections;
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
            var response = await entityRelationService.GetAll();
            return await response.ToListAsync();
        }

        public async Task<List<EntityRelation>> GetCurrentEntityRelations() //TODO change to stream call
        {
            var version = await versionService.GetCurrentVersion();
            var response = await entityRelationService.GetByVersion(version.VersionID);
            return await response.ToListAsync();
        }

        public async Task<List<EntityRelation>> GetPreviousEntityRelations() //TODO change to stream call
        {
            var version = await mappingService.GetPreviousVersionIDWithEntityRelations();
            var response = await entityRelationService.GetByVersion(version.VersionID);
            return await response.ToListAsync();
        }

        public async Task CreateEntityRelation(int versionNumber, List<PutEntityRelationRequest> entityRelationsRequest) //TODO change to stream call
        {
            await mappingService.CreateEntityRelation(versionNumber, entityRelationsRequest);
        }

        public async Task CreateOne(EntityRelation entityRelationsRequest) //TODO change to stream call
        {
            await entityRelationService.Create(entityRelationsRequest);
        }
    }
}
