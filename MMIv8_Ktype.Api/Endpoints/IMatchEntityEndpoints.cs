using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Attributes;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using MongoDB.Driver;
using Refit;

namespace MMIv8_Ktype.Api.Endpoints
{
    [GroupName("MatchEntity")]
    public interface IMatchEntityEndpoints : IBaseEndpoint<MatchEntity, ObjectId, MatchEntityFilterRequest, MatchEntitySortRequest>, IVersionEndpoint<MatchEntity, ObjectId, MatchEntityFilterRequest>, IEndpoint
    {
        [Get("/MatchSummary")]
        Task<SerializableResult<PagedCursorResponse<MatchEntitySummary>>> GetAllMatchEntitySummary([AsParameters] PagedCursorRequest<ObjectId> PagedRequest, [AsParameters] MatchEntityFilterRequest Filter);

        //[Get("/MatchRefine")]
        //Task<SerializableResult<List<MatchRefine>>> GetAllMatchRefine([AsParameters] MatchEntityFilterRequest Filter);

        [Get("/DistinctMMIv8")]
        Task<SerializableResult<List<MMI_V8_Key>>> GetDistinctMMIv8([AsParameters] MatchEntityFilterRequest Filter);

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
        Task<SerializableResult<PagedCursorResponse<MatchEntityBackup>>> GetMatchEntityBackup([AsParameters] PagedCursorRequest<ObjectId> PagedRequest);

        [Delete("/Debug/DeleteAll")]
        Task<Result> DeleteAll();
    }
}
