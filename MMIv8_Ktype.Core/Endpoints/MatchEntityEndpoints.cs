using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Models.Collections;
using MongoDB.Driver;

namespace MMIv8_Ktype.Core.Endpoints
{
    public class MatchEntityEndpoints(MatchEntityService matchEntityService,
                                      MappingService mappingService) : IMatchEntityEndpoints
    {
        public async Task<PagedResponse<MatchEntity>> GetAll(PagedRequest pagedRequest) //TODO change to stream call
        {
            return await matchEntityService.PaginateDocuments(page: pagedRequest.Page, pageSize: pagedRequest.PageSize ?? 100);
        }

        public async Task<PagedResponse<MatchEntity>> GetAllMatchRefine(PagedRequest pagedRequest) //TODO change to stream call
        {
            var filterBuilder = Builders<MatchEntity>.Filter;
            var filter = filterBuilder.Eq(c => c.MatchResult.Failed, false)
                       & (filterBuilder.Eq(c => c.MatchRefine.IsCheck, true) | filterBuilder.Size(c => c.MatchRefine.ChosenMatches, 0));


            return await matchEntityService.PaginateDocuments(filter: filter, page: pagedRequest.Page, pageSize: pagedRequest.PageSize ?? 100);
        }

        public async Task<MatchEntity?> GetMatchEntity(MatchEntityByExternalRequest request)
        {
            return await matchEntityService.GetByExternalIds(request.KtypNr, request.MMI_V8_Key);
        }

        public async Task UpdateMatchedFlag(UpdateFlagRequest request)
        {
            await mappingService.UpdateMatchedFlag([ request ]); // TODO Change to single MMI_V8_Key
        }

        public async Task UpdateFailedFlag(UpdateFlagRequest request)
        {
            await mappingService.UpdateFailedFlag([ request ]); // TODO Change to single MMI_V8_Key
        }

        public async Task UpdateMatchRefineStatus(int[] MMI_V8_Keys) // TODO Change to single MMI_V8_Key
        {
            await mappingService.UpdateMatchRefineStatus(MMI_V8_Keys);
        }

        public async Task<MatchEntity?> CheckEntityMatch(MatchEntityByExternalRequest request)
        {
            return await mappingService.CheckMatchVadlidity(request.KtypNr, request.MMI_V8_Key);
        }
    }
}
