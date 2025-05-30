using Microsoft.AspNetCore.Mvc;
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
        public async Task<PagedCursorResponse<MatchBase>> GetAll(string? cursor = null, int PageSize = 100)
        {
            return await matchBaseService.PaginateDocumentsByCursor<MatchBase, string>(cursor: cursor, pageSize: PageSize);
        }

        public async Task<PagedCursorResponse<MatchBase>> GetByMatchBaseType(MatchBaseType MatchBaseType, string? cursor = null, int PageSize = 100) //TODO change to stream call
        {
            var filter = Builders<MatchBase>.Filter.Eq(c => c.MatchBaseType, MatchBaseType);

            return await matchBaseService.PaginateDocumentsByCursor<MatchBase, string>(filter: filter, cursor: cursor, pageSize: PageSize);
        }

        public async Task<MatchBase?> GetById(string MatchHash)
        {
            return await matchBaseService.GetById(MatchHash);
        }

        public async Task UpdateStatus(MatchBase request, StatusChange newStatus)
        {
            await matchBaseService.Update(request).AppendPipeline(c => c.AppendStatus(newStatus)).UpdateDocuments();
        }

        public async Task UpdateMatchBaseScore(MatchBaseType MatchBaseType, string MatchHash, decimal NewScore)
        {
            await mappingService.UpdateMatchScore(MatchBaseType, MatchHash, NewScore);
        }

        public async Task StorePartialMatchBase(MatchBaseType MatchBaseType, string MatchHash, decimal NewScore)
        {
            await mappingService.StorePartialMatchBase(MatchBaseType, MatchHash, NewScore);
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
