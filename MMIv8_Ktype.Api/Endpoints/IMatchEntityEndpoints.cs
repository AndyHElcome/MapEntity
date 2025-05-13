using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Models.Attributes;
using MMIv8_Ktype.Models.Collections;
using MongoDB.Driver;
using Refit;

namespace MMIv8_Ktype.Api.Endpoints
{
    [GroupName("MatchEntity")]
    public interface IMatchEntityEndpoints : IEndpoint
    {
        [Post("")]
        Task<MatchEntity?> GetMatchEntity(MatchEntityByExternalRequest request);

        [Post("/CheckEntityMatch")]
        Task<MatchEntity?> CheckEntityMatch(MatchEntityByExternalRequest request);

        [Get("/GetAll")]
        Task<PagedResponse<MatchEntity>> GetAll([FromQuery] int Page = 1, [FromQuery] int PageSize = 100);

        [Post("/MatchRefine/GetAll")]
        Task<PagedResponse<MatchEntity>> GetAllMatchRefine([FromQuery] int Page = 1, [FromQuery] int PageSize = 100);

        [Put("/UpdateFailedFlag")]
        Task UpdateFailedFlag(UpdateFlagRequest request);

        [Put("/UpdateMatchedFlag")]
        Task UpdateMatchedFlag(UpdateFlagRequest request);

        [Put("/UpdateMatchRefineStatus")]
        Task UpdateMatchRefineStatus([FromBody] int[] MMI_V8_Keys);

        [Post("/Debug/GetMatchEntityBackup")]
        Task<PagedResponse<MatchEntityBackup>> GetMatchEntityBackup([FromQuery] int Page = 1, [FromQuery] int PageSize = 100);

        [Delete("/Debug/DeleteAll")]
        Task DeleteAll();
    }
}
