using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Core.Services;
using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Models.Util;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace MMIv8_Ktype.Core.Endpoints
{
    public class MatchBaseEndpoints(MatchBaseService matchBaseService,
                                    MappingService mappingService,
                                    IVersionProvider versionProvider) : IMatchBaseEndpoints
    {
        public async Task<List<MatchBase>> GetAll() //TODO change to stream call
        {
            return await matchBaseService.GetFindFluent().ToListAsync();
        }

        public async Task<List<MatchBase>> GetByMatchBaseType(MatchBaseType MatchBaseType) //TODO change to stream call
        {
            return await matchBaseService.GetByMatchBaseType(MatchBaseType);
        }

        public async Task<MatchBase?> GetById(string MatchHash)
        {
            return await matchBaseService.GetById(MatchHash);
        }

        public async Task UpdateStatus(MatchBase request, Status status, string? detail = null)
        {
            await matchBaseService.Update(request).AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(status, detail))).UpdateDocuments();
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

        public async Task DeleteAll()
        {
            await matchBaseService.DeleteAll();
        }
    }
}
