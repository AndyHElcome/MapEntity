using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MMIv8_Ktype.Api;
using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Outputs;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Reflection.Metadata;

namespace MMIv8_Ktype.Core.Endpoints
{
    public class MatchEntityEndpoints(MatchEntityService matchEntityService,
                                      MappingService mappingService) : IMatchEntityEndpoints
    {
        private static FilterDefinition<MatchEntity> MatchEntityFilter(
            string? MakeModelMatchId = null,
            string? TecDocEntityId = null,
            string? MMIv8EntityId = null,
            bool? IsCheck = null,
            bool? IsMatched = null,
            bool? IsFailed = null,
            bool? HasDifference = null,
            Status[]? Status = null)
        {
            var filterBuilder = Builders<MatchEntity>.Filter;
            var filter = filterBuilder.Empty;

            if (MakeModelMatchId is not null)
                filter = filter & filterBuilder.Eq(c => c.MatchMakeModelMatchID, ObjectId.Parse(MakeModelMatchId));

            if (TecDocEntityId is not null)
                filter = filter & filterBuilder.Eq(c => c.TecDocEntity.DocumentId, ObjectId.Parse(TecDocEntityId));

            if (MMIv8EntityId is not null)
                filter = filter & filterBuilder.Eq(c => c.MMIv8Entity.DocumentId, ObjectId.Parse(MMIv8EntityId));

            if (IsCheck is not null)
                filter = filter & filterBuilder.Eq(c => c.MatchRefine.IsCheck, IsCheck);

            if (IsMatched is not null)
                filter = filter & filterBuilder.Eq(c => c.Matched, IsMatched);

            if (IsFailed is not null)
                filter = filter & filterBuilder.Eq(c => c.MatchResult.Failed, IsFailed);

            if (HasDifference is not null)
                filter = filter & filterBuilder.Eq(c => c.MatchRefine.Difference, HasDifference);

            if (Status is not null && Status.Length != 0)
                filter = filter & filterBuilder.In(c => c.Status.Current.Status, Status);

            return filter;
        }

        public async Task<MatchEntity> GetById(ObjectId DocumentId)
        {
            return await matchEntityService.GetById(DocumentId);
        }

        public async Task<PagedCursorResponse<MatchEntity>> GetAll(
            string? cursor = null,
            int PageSize = 100,
            string? MakeModelMatchId = null,
            string? TecDocEntityId = null,
            string? MMIv8EntityId = null,
            bool? IsCheck = null,
            bool? IsMatched = null,
            bool? IsFailed = null,
            bool? HasDifference = null,
            Status[]? Status = null)
        {
            var filter = MatchEntityFilter(MakeModelMatchId, TecDocEntityId, MMIv8EntityId, IsCheck, IsMatched, IsFailed, HasDifference, Status);
            var objectId = ObjectId.TryParse(cursor, out var objectid) ? objectid : ObjectId.Empty;

            return await matchEntityService.PaginateDocumentsByCursor<MatchEntity, ObjectId>(filter: filter, cursor: objectId, pageSize: PageSize);
        }

        public async Task<PagedCursorResponse<MatchEntitySummary>> GetAllMatchEntitySummary(
            string? cursor = null,
            int PageSize = 100,
            string? MakeModelMatchId = null,
            string? TecDocEntityId = null,
            string? MMIv8EntityId = null,
            bool? IsCheck = null,
            bool? IsMatched = null,
            bool? IsFailed = null,
            bool? HasDifference = null,
            Status[]? Status = null)
        {
            var filter = MatchEntityFilter(MakeModelMatchId, TecDocEntityId, MMIv8EntityId, IsCheck, IsMatched, IsFailed, HasDifference, Status);
            var objectId = ObjectId.TryParse(cursor, out var objectid) ? objectid : ObjectId.Empty;

            var projection = Builders<MatchEntity>.Projection.Expression(c
                => new MatchEntitySummary(
                    c.DocumentId,
                    c.MatchMakeModelMatchID,
                    c.TecDocEntity.DocumentId,
                    c.MMIv8Entity.DocumentId,
                    c.MatchResult.Failed,
                    c.MatchResult.FailDetail,
                    c.MatchResult.PreviousMatch,
                    c.MatchRefine.IsCheck,
                    c.MatchRefine.Difference,
                    c.MatchRefine.BestScore,
                    c.ScoreSum,
                    c.IsBest,
                    c.Matched,
                    c.MatchDetail,
                    c.Status.Current.Status)
                );

            return await matchEntityService.PaginateDocumentsByCursor<MatchEntitySummary, ObjectId>(filter: filter, cursor: objectId, pageSize: PageSize, projection: projection);
        }

        public async Task<List<MatchRefine>> GetAllMatchRefine(
            string? MakeModelMatchId = null,
            string? TecDocEntityId = null,
            string? MMIv8EntityId = null,
            bool? IsCheck = null,
            bool? IsMatched = null,
            bool? IsFailed = null,
            bool? HasDifference = null,
            Status[]? Status = null)
        {
            var filter = MatchEntityFilter(MakeModelMatchId, TecDocEntityId, MMIv8EntityId, IsCheck, IsMatched, IsFailed, HasDifference, Status);

            return await matchEntityService.GetDistinctDocuments<MatchRefine>(nameof(MatchEntity.MatchRefine), filter);
        }

        public async Task<PagedCursorResponse<MatchEntityBackup>> GetMatchEntityBackup(string? cursor = null, int PageSize = 100)
        {
            var filterBuilder = Builders<MatchEntity>.Filter;
            var filter = filterBuilder.Eq(c => c.Matched, true)
                       | filterBuilder.Eq(c => c.Status.Current.Status, Models.Status.Status.Check)
                       | filterBuilder.Eq(c => c.Status.Current.Status, Models.Status.Status.Checked);
            var objectId = ObjectId.TryParse(cursor, out var objectid) ? objectid : ObjectId.Empty;

            var projection = Builders<MatchEntity>.Projection.Expression(c 
                => new MatchEntityBackup(c.DocumentId,
                                         c.MMIv8Entity.MMI_V8_Key,
                                         c.TecDocEntity.KTypNr, 
                                         c.Matched, 
                                         c.MatchDetail ?? "", 
                                         c.MatchResult.Failed, 
                                         c.MatchResult.FailDetail ?? "", 
                                         c.Status.Current.Status));

            return await matchEntityService.PaginateDocumentsByCursor<MatchEntityBackup, ObjectId>(filter: filter, cursor: objectId, pageSize: PageSize, projection: projection);
        }

        public async Task<MatchEntity?> GetMatchEntity(MatchEntityByExternalRequest request)
        {
            return await matchEntityService.GetByExternalIds(request.KtypNr, request.MMI_V8_Key);
        }

        public async Task UpdateMatchedFlag(UpdateFlagRequest request)
        {
            await mappingService.UpdateMatchedFlag(request);
        }

        public async Task UpdateFailedFlag(UpdateFlagRequest request)
        {
            await mappingService.UpdateFailedFlag(request);
        }

        public async Task UpdateMatchRefineStatus(int MMI_V8_Key)
        {
            await mappingService.UpdateMatchRefineStatus(MMI_V8_Key);
        }

        public async Task ResetMatchResult(int MMI_V8_Key)
        {
            await mappingService.ResetMatchResult(MMI_V8_Key);
        }

        public async Task<MatchEntity?> CheckEntityMatch(MatchEntityByExternalRequest request)
        {
            return await mappingService.CheckMatchVadlidity(request.KtypNr, request.MMI_V8_Key);
        }

        public async Task DeleteAll()
        {
            await matchEntityService.DeleteAll();
        }
    }
}
