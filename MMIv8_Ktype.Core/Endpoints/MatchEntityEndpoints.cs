using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MMIv8_Ktype.Api;
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
using System.Reflection.Metadata;
using System.Security.Cryptography;

namespace MMIv8_Ktype.Core.Endpoints
{
    public class MatchEntityEndpoints(MatchEntityService matchEntityService, MappingService mappingService) : BaseEndpointsWithVersion<MatchEntity, ObjectId, MatchEntityFilterRequest, MatchEntitySortRequest>(matchEntityService), IMatchEntityEndpoints
    {

        public async Task<SerializableResult<PagedCursorResponse<MatchEntitySummary>>> GetAllMatchEntitySummary([AsParameters] PagedCursorRequest<ObjectId> PagedRequest, [AsParameters] MatchEntityFilterRequest Filter)
        {
            var projection = Builders<MatchEntity>.Projection.Expression(c
                => new MatchEntitySummary(
                    c.DocumentId,
                    c.MatchMakeModelMatchID,
                    c.TecDocEntity.DocumentId,
                    c.MMIv8Entity.DocumentId,
                    c.MatchResult.Failed,
                    c.MatchResult.FailDetail,
                    c.MatchResult.PreviousMatch,
                    c.MMIv8Entity.MatchRefine.IsCheck,
                    c.MMIv8Entity.MatchRefine.Difference,
                    c.MMIv8Entity.MatchRefine.BestScore,
                    c.ScoreSum,
                    c.MMIv8Entity.MatchRefine.BestScore != null && c.ScoreSum == c.MMIv8Entity.MatchRefine.BestScore, //c.IsBest,
                    c.Matched,
                    c.MatchDetail,
                    c.MatchResult.ComparisonCount == c.MatchResult.PerfectCount, //c.MatchResult.IsPerfect,
                    c.Status.Current.Status)
                );

            return await matchEntityService.PaginateDocumentsByCursor<MatchEntitySummary, ObjectId>(filter: Filter.GetFilter(), cursor: PagedRequest.GetCursor(), pageSize: PagedRequest.PageSize ?? 0, projection: projection);
        }

        //public async Task<SerializableResult<List<MatchRefine>>> GetAllMatchRefine([AsParameters] MatchEntityFilterRequest Filter)
        //{
        //    return await matchEntityService.GetDistinctDocuments<MatchRefine>(nameof(MatchEntity.MatchRefine), Filter.GetFilter());
        //}

        public async Task<SerializableResult<PagedCursorResponse<MatchEntityBackup>>> GetMatchEntityBackup([AsParameters] PagedCursorRequest<ObjectId> PagedRequest)
        {
            var filterBuilder = Builders<MatchEntity>.Filter;
            var filter =
                filterBuilder.Or(
                    filterBuilder.Eq(c => c.Matched, true),
                    filterBuilder.Eq(c => c.MatchResult.Failed, true) & filterBuilder.Eq(c => c.MatchResult.FailCount, 0)
                    );

            var projection = Builders<MatchEntity>.Projection.Expression(c
                => new MatchEntityBackup(c.DocumentId,
                                         c.MMIv8Entity.MMI_V8_Key,
                                         c.TecDocEntity.KTypNr,
                                         c.Matched,
                                         c.MatchDetail ?? "",
                                         c.MatchResult.Failed,
                                         c.MatchResult.FailDetail ?? "",
                                         c.Status.Current.Status));

            return await matchEntityService.PaginateDocumentsByCursor<MatchEntityBackup, ObjectId>(filter: filter, cursor: PagedRequest.GetCursor(), pageSize: PagedRequest.PageSize ?? 0, projection: projection);
        }

        public async Task<SerializableResult<List<MMI_V8_Key>>> GetDistinctMMIv8([AsParameters] MatchEntityFilterRequest Filter)
        {
            var results = await matchEntityService.GetDistinctDocuments<int>(nameof(MatchEntity.MMIv8Entity) + '.' + nameof(SourceMMIv8.ExternalId), filter: Filter.GetFilter());

            if (!results.IsSuccess)
                return results.Error!;

            return Result.Success(results.Value.ConvertAll(c => new MMI_V8_Key(c)));
        }

        public async Task<SerializableResult<MatchEntity>> GetMatchEntity(int KtypNr, int MMI_V8_Key)
        {
            return await matchEntityService.GetByExternalIds(KtypNr, MMI_V8_Key);
        }

        public async Task<SerializableResult<MatchEntity>> CheckEntityMatch(int KtypNr, int MMI_V8_Key)
        {
            return await mappingService.CheckMatchVadlidity(KtypNr, MMI_V8_Key);
        }

        public async Task<Result> UpdateMatchedFlag(UpdateFlagRequest request)
        {
            return await mappingService.UpdateMatchedFlag(request);
        }

        public async Task<Result> UpdateFailedFlag(UpdateFlagRequest request)
        {
            return await mappingService.UpdateFailedFlag(request);
        }

        public async Task<Result> UpdateMatchRefineStatus(int MMI_V8_Key)
        {
            return await mappingService.UpdateMatchRefineStatus(MMI_V8_Key);
        }

        public async Task<Result> ResetMatchResult(int MMI_V8_Key)
        {
            return await mappingService.ResetMatchResult(MMI_V8_Key);
        }

        public async Task<Result> DeleteAll()
        {
            return await matchEntityService.DeleteAll();
        }
    }
}
