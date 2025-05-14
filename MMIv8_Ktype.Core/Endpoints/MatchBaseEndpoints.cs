using Microsoft.AspNetCore.Mvc.RazorPages;
using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
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
                                    MappingService mappingService) : IMatchBaseEndpoints
    {
        public async Task<PagedResponse<MatchBase>> GetAll(int Page = 1, int PageSize = 100)
        {
            return await matchBaseService.PaginateDocuments<MatchBase>(page: Page, pageSize: PageSize);
        }

        public async Task<PagedResponse<MatchBase>> GetByMatchBaseType(MatchBaseType MatchBaseType, int Page = 1, int PageSize = 100) //TODO change to stream call
        {
            var filter = Builders<MatchBase>.Filter.Eq(c => c.MatchBaseType, MatchBaseType);

            return await matchBaseService.PaginateDocuments<MatchBase>(filter, page: Page, pageSize: PageSize);
        }

        public async Task<MatchBase?> GetById(string MatchHash)
        {
            return await matchBaseService.GetById(MatchHash);
        }

        public async Task UpdateStatus(MatchBase request, StatusChange newStatus)
        {
            await matchBaseService.Update(request).AppendPipeline(c => c.AppendStatus(newStatus)).UpdateDocuments();
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
