using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Util;
using MongoDB.Driver;

namespace MMIv8_Ktype.Core.Endpoints
{
    public class MatchBaseEndpoints(MatchBaseService matchBaseService,
                                    MappingService mappingService) : IMatchBaseEndpoints
    {
        public async Task<List<MatchBase>> GetAll() //TODO change to stream call
        {
            var response = await matchBaseService.GetCursor();
            return await response.ToListAsync();
        }

        public async Task<List<MatchBase>> GetByMatchBaseType(MatchBaseType MatchBaseType) //TODO change to stream call
        {
            var response = await matchBaseService.GetByType(MatchBaseType);
            return await response.ToListAsync();
        }

        public async Task<MatchBase?> GetById(string MatchHash)
        {
            return await matchBaseService.GetById(MatchHash);
        }

        public async Task UpdateMatchBaseScore(PutMatchBaseRequest request)
        {
            await mappingService.UpdateMatchScore(request.MatchBaseType, request.MatchHash, request.NewScore);
        }

        public async Task StorePartialMatchBase(PutMatchBaseRequest request)
        {
            await mappingService.StorePartialMatchBase(request.MatchBaseType, request.MatchHash, request.NewScore);
        }

        public async Task RemovePartialMatchBase(MatchBaseType MatchBaseType, string MatchHash)
        {
            await mappingService.RemovePartialMatchBase(MatchBaseType, MatchHash);
        }
    }
}
