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

namespace MMIv8_Ktype.Core.Endpoints
{
    public class MatchMakeModelEndPoints(MatchMakeModelService matchMakeModelService, 
                                         MappingService mappingService,
                                         BulkMappingService bulkMappingService) : IMatchMakeModelEndpoints
    {
        public async Task<PagedResponse<MatchMakeModel>> GetAll(int Page = 1, int PageSize = 100) //TODO Fix large gets
        {
            return await matchMakeModelService.PaginateDocuments<MatchMakeModel>(page: Page, pageSize: PageSize);
        }

        public async Task<List<MatchMakeModel>> GenerateMakeModelMatch()
        {
            return await bulkMappingService.GenerateMakeModelMatch();
        }

        public async Task<MatchMakeModel?> GetMakeModelMatch(string TD_SourceEntityModelHash, string MMI_SourceEntityModelHash)
        {
            return await matchMakeModelService.GetByModelIds(new(TD_SourceEntityModelHash, MMI_SourceEntityModelHash));
        }

        public async Task<List<MatchMakeModel>> GetByModelId(SourceIndex SourceIndex, string SourceEntityModelHash)
        {
            return await matchMakeModelService.GetByModelId(SourceIndex, SourceEntityModelHash);
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
