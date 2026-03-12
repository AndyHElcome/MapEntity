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
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace MMIv8_Ktype.Core.Endpoints
{
    public class MatchBaseEndpoints(MatchBaseService matchBaseService,
                                    MappingService mappingService) : BaseEndpointsWithVersion<MatchBase, string, MatchBaseFilterRequest, SortQuery<MatchBase>>(matchBaseService), IMatchBaseEndpoints
    {


        //TODO move this into a filterQuery
        public async Task<SerializableResult<PagedCursorResponse<MatchBase>>> GetByMatchBaseType(MatchBaseType MatchBaseType, [AsParameters] PagedCursorRequest<string> PagedRequest) //TODO change to stream call
        {
            var filter = Builders<MatchBase>.Filter.Eq(c => c.MatchBaseType, MatchBaseType);

            return await matchBaseService.PaginateDocumentsByCursor<MatchBase, string>(filter: filter, cursor: PagedRequest.GetCursor(), pageSize: PagedRequest.PageSize ?? 0);
        }

        public async Task<Result> UpdateMatchBaseScore(MatchBaseType MatchBaseType, string MatchHash, decimal NewScore)
        {
            return await mappingService.UpdateMatchScore(MatchHash, NewScore);
        }

        public async Task<Result> AddMatchBaseContext(string MatchHash, AddMatchContext MatchContext)
        {
            if (MatchContext.TecDocEntity is null && MatchContext.MMIEntity is null)
                return Error.Validation("MatchBase.MatchContext.AddValidation", "Both TecDocEntity and MMIEntity are null");

            return await mappingService.AddMatchContext(MatchHash, new MatchContext(MatchContext.TecDocEntity, MatchContext.MMIEntity, MatchContext.ScoreOverride));
        }

        public async Task<Result> RemoveMatchBaseContext(string MatchHash, RemoveMatchContext MatchContext)
        {
            if (MatchContext.ContextId is null && MatchContext.TecDocEntity is null && MatchContext.MMIEntity is null)
                return Error.Validation("MatchBase.MatchContext.AddValidation", "All of ContextId, TecDocEntity and MMIEntity are null");

            string contextToRemove = MatchContext.ContextId ?? new MatchContext(MatchContext.TecDocEntity, MatchContext.MMIEntity, 0).ContextId;

            return await mappingService.RemoveMatchContext(MatchHash, contextToRemove);
        }

        public async Task<Result> ReorderMatchBaseContext(string MatchHash, string[] MatchContextsOrder)
        {
            return await mappingService.ReorderMatchContext(MatchHash, MatchContextsOrder);
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
