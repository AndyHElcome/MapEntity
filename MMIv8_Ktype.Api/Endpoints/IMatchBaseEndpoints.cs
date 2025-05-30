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
        Task<PagedCursorResponse<MatchBase>> GetAll([FromQuery] string? cursor = null, [FromQuery] int PageSize = 100);

        [Get("/GetByType/{MatchBaseType}")]
        Task<PagedCursorResponse<MatchBase>> GetByMatchBaseType([FromRoute] MatchBaseType MatchBaseType, [FromQuery] string? cursor = null, [FromQuery] int PageSize = 0);

        [Get("/GetById/{MatchHash}")]
        Task<MatchBase?> GetById(string MatchHash);

        [Put("/{MatchBaseType}")]
        Task UpdateMatchBaseScore([FromRoute] MatchBaseType MatchBaseType, [FromQuery] string MatchHash, [FromQuery] decimal NewScore);

        [Put("/Partial/{MatchBaseType}")]
        Task StorePartialMatchBase([FromRoute] MatchBaseType MatchBaseType, [FromQuery] string MatchHash, [FromQuery] decimal NewScore);

        [Delete("/Partial/{MatchBaseType}")]
        Task RemovePartialMatchBase([FromRoute] MatchBaseType MatchBaseType, [FromQuery] string MatchHash);

        [Delete("/Debug/DeleteAll")]
        Task DeleteAll();
    }
}
