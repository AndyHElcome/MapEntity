using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Attributes;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Util;
using MongoDB.Driver;
using Refit;

namespace MMIv8_Ktype.Api.Endpoints
{
    [GroupName("MatchBase")]
    public interface IMatchBaseEndpoints : IEndpoint
    {
        [Get("")]
        Task<SerializableResult<PagedCursorResponse<MatchBase>>> GetAll([FromQuery] string? cursor = null, [FromQuery] int PageSize = 100);

        [Get("/GetByType/{MatchBaseType}")]
        Task<SerializableResult<PagedCursorResponse<MatchBase>>> GetByMatchBaseType([FromRoute] MatchBaseType MatchBaseType, [FromQuery] string? cursor = null, [FromQuery] int PageSize = 0);

        [Get("/GetById/{MatchHash}")]
        Task<SerializableResult<MatchBase>> GetById(string MatchHash);

        [Put("/{MatchBaseType}")]
        Task<Result> UpdateMatchBaseScore([FromRoute] MatchBaseType MatchBaseType, [FromQuery] string MatchHash, [FromQuery] decimal NewScore);

        [Put("/Partial/{MatchBaseType}")]
        Task<Result> StorePartialMatchBase([FromRoute] MatchBaseType MatchBaseType, [FromQuery] string MatchHash, [FromQuery] decimal NewScore);

        [Delete("/Partial/{MatchBaseType}")]
        Task<Result> RemovePartialMatchBase([FromRoute] MatchBaseType MatchBaseType, [FromQuery] string MatchHash);

        [Delete("/Debug/DeleteAll")]
        Task<Result> DeleteAll();

        [Post("/Debug/{MatchBaseType}/RecalculateAutomaticMatchBaseScore")]
        Task<Result> RecalculateAutomaticMatchBaseScore(MatchBaseType MatchBaseType);
    }
}
