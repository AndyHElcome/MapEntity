using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Models.Attributes;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Util;
using Refit;

namespace MMIv8_Ktype.Api.Endpoints
{
    [GroupName("MatchBase")]
    public interface IMatchBaseEndpoints : IEndpoint
    {
        [Get("")]
        Task<PagedResponse<MatchBase>> GetAll([FromQuery] int Page, [FromQuery] int PageSize);

        [Get("/GetByType/{MatchBaseType}")]
        Task<PagedResponse<MatchBase>> GetByMatchBaseType([FromQuery] MatchBaseType MatchBaseType, [FromQuery] int Page, [FromQuery] int PageSize, [FromQuery] PageCount pageCount);

        [Get("/GetById/{MatchHash}")]
        Task<MatchBase?> GetById(string MatchHash);

        [Put("")]
        Task UpdateMatchBaseScore(PutMatchBaseRequest request);

        [Put("/Partial/")]
        Task StorePartialMatchBase(PutMatchBaseRequest request);

        [Delete("/Partial/{MatchBaseType}/{MatchHash}")]
        Task RemovePartialMatchBase(MatchBaseType MatchBaseType, string MatchHash);

        [Delete("/Debug/DeleteAll")]
        Task DeleteAll();
    }
}
