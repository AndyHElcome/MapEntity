using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Attributes;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Outputs;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using MongoDB.Driver;
using Refit;

namespace MMIv8_Ktype.Api.Endpoints
{
    [GroupName("MatchEntity")]
    public interface IMatchEntityEndpoints : IEndpoint
    {
        [Get("/{DocumentId}")]
        Task<SerializableResult<MatchEntity>> GetById(ObjectId DocumentId);

        [Get("")]
        Task<SerializableResult<PagedCursorResponse<MatchEntity>>> GetAll(
            string? cursor = null,
            int PageSize = 100,
            string? MakeModelMatchId = null,
            string? TecDocEntityId = null,
            string? MMIv8EntityId = null,
            bool? IsCheck = null,
            bool? IsMatched = null,
            bool? IsFailed = null,
            bool? HasDifference = null,
            Status[]? Status = null);

        [Get("/MatchSummary")]
        Task<SerializableResult<PagedCursorResponse<MatchEntitySummary>>> GetAllMatchEntitySummary(
            string? cursor = null,
            int PageSize = 100,
            string? MakeModelMatchId = null,
            string? TecDocEntityId = null,
            string? MMIv8EntityId = null,
            bool? IsCheck = null,
            bool? IsMatched = null,
            bool? IsFailed = null,
            bool? HasDifference = null,
            Status[]? Status = null);

        [Get("/MatchRefine")]
        Task<SerializableResult<List<MatchRefine>>> GetAllMatchRefine(
            string? MakeModelMatchId = null,
            string? TecDocEntityId = null,
            string? MMIv8EntityId = null,
            bool? IsCheck = null,
            bool? IsMatched = null,
            bool? IsFailed = null,
            bool? HasDifference = null,
            Status[]? Status = null);
        
        [Post("/CheckEntityMatch")] //TODO Change to add the match into the collection
        Task<SerializableResult<MatchEntity>> CheckEntityMatch([FromQuery] int KtypNr, [FromQuery] int MMI_V8_Key);

        [Put("/UpdateFailedFlag")]
        Task<Result> UpdateFailedFlag(UpdateFlagRequest request);

        [Put("/UpdateMatchedFlag")]
        Task<Result> UpdateMatchedFlag(UpdateFlagRequest request);

        [Put("/UpdateMatchRefineStatus/{MMI_V8_Key}")]
        Task<Result> UpdateMatchRefineStatus([FromRoute] int MMI_V8_Key);

        [Put("/ResetMatchResult/{MMI_V8_Key}")]
        Task<Result> ResetMatchResult([FromRoute] int MMI_V8_Key);

        [Get("/Debug/GetMatchEntityBackup")]
        Task<SerializableResult<PagedCursorResponse<MatchEntityBackup>>> GetMatchEntityBackup(string? cursor = null, int PageSize = 100);

        [Delete("/Debug/DeleteAll")]
        Task<SerializableResult<DeleteResult>> DeleteAll();
    }
}
