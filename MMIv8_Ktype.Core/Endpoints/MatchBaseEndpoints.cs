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
        public async Task<SerializableResult<PagedCursorResponse<MatchBase>>> GetAll(string? cursor = null, int PageSize = 100)
        {
            return await matchBaseService.PaginateDocumentsByCursor<MatchBase, string>(cursor: cursor, pageSize: PageSize);
        }

        public async Task<SerializableResult<PagedCursorResponse<MatchBase>>> GetByMatchBaseType(MatchBaseType MatchBaseType, string? cursor = null, int PageSize = 100) //TODO change to stream call
        {
            var filter = Builders<MatchBase>.Filter.Eq(c => c.MatchBaseType, MatchBaseType);

            return await matchBaseService.PaginateDocumentsByCursor<MatchBase, string>(filter: filter, cursor: cursor, pageSize: PageSize);
        }

        public async Task<SerializableResult<MatchBase>> GetById(string MatchHash)
        {
            return await matchBaseService.GetById(MatchHash);
        }

        public async Task<SerializableResult<MatchBase>> UpdateStatus(MatchBase MatchBase, Status Status, string? Detail = null)
        {
            return await matchBaseService.UpdateStatus(MatchBase, Status, Detail);
        }

        public async Task<Result> UpdateMatchBaseScore(MatchBaseType MatchBaseType, string MatchHash, decimal NewScore)
        {
            return await mappingService.UpdateMatchScore(MatchBaseType, MatchHash, NewScore);
        }

        public async Task<Result> StorePartialMatchBase(MatchBaseType MatchBaseType, string MatchHash, decimal NewScore)
        {
            return await mappingService.StorePartialMatchBase(MatchBaseType, MatchHash, NewScore);
        }

        public async Task<Result> RemovePartialMatchBase(MatchBaseType MatchBaseType, string MatchHash)
        {
            return await mappingService.RemovePartialMatchBase(MatchBaseType, MatchHash);
        }

        public async Task<Result> RecalculateAutomaticMatchBaseScore(MatchBaseType MatchBaseType)
        {
            var filter = Builders<MatchEntity>.Filter.Empty;
            return await mappingService.RecalculateAutomaticMatchBaseScore(filter, MatchBaseType);
        }

        public async Task<Result> DeleteAll()
        {
            return await matchBaseService.DeleteAll();
        }
    }
}
