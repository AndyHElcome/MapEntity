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

        [Post("/GetAll")]
        Task<PagedResponse<MatchEntity>> GetAll(PagedRequest pagedRequest);

        [Post("/MatchRefine/GetAll")]
        Task<PagedResponse<MatchEntity>> GetAllMatchRefine(PagedRequest pagedRequest);

        [Put("/UpdateFailedFlag")]
        Task UpdateFailedFlag(UpdateFlagRequest request);

        [Put("/UpdateMatchedFlag")]
        Task UpdateMatchedFlag(UpdateFlagRequest request);

        [Put("/UpdateMatchRefineStatus")]
        Task UpdateMatchRefineStatus([FromBody] int[] MMI_V8_Keys);

        [Post("/Debug/GetMatchEntityBackup")]
        Task<PagedResponse<MatchEntityBackup>> GetMatchEntityBackup(PagedRequest pagedRequest);

        [Delete("/Debug/DeleteAll")]
        Task DeleteAll();
    }
}
