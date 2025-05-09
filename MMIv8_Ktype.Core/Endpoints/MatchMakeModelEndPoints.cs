using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Models.Collections;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MMIv8_Ktype.Core.Endpoints
{
    public class MatchMakeModelEndPoints(MatchMakeModelService matchMakeModelService, 
                                         MappingService mappingService,
                                         BulkMappingService bulkMappingService) : IMatchMakeModelEndpoints
    {
        public async Task<List<MatchMakeModel>> GetAllMakeModelMatch() //TODO Fix large gets
        {
            var response = await matchMakeModelService.GetCursor();

            return await response.ToListAsync();
        }

        public async Task<List<MatchMakeModel>> GenerateMakeModelMatch()
        {
            return await bulkMappingService.GenerateMakeModelMatch();
        }

        public async Task<MatchMakeModel?> GetMakeModelMatch(MatchMakeModelRequest request)
        {
            return await matchMakeModelService.GetByModelIds(request);
        }

        public async Task<MatchMakeModel?> GetMakeModelMatchById(ObjectId MatchID)
        {
            return await matchMakeModelService.GetById(MatchID);
        }

        public async Task CreateMakeModelMatch(MatchMakeModelRequest request)
        {
            _ = await mappingService.CreateMakeModelMatch(request);
        }

        public async Task DeleteMakeModelMatch(ObjectId MatchID)
        {
            _ = await mappingService.DeleteMakeModelMatch(MatchID);
        }

        public async Task DeleteAll()
        {
            _ = await matchMakeModelService.DeleteAll();
        }
    }
}
