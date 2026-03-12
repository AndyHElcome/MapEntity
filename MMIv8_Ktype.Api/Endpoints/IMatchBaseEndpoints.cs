using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Attributes;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using MongoDB.Driver;
using Refit;
using System.Security.Cryptography;

namespace MMIv8_Ktype.Api.Endpoints
{
    [GroupName("MatchBase")]
    public interface IMatchBaseEndpoints : IBaseEndpoint<MatchBase, string, MatchBaseFilterRequest, SortQuery<MatchBase>>, IVersionEndpoint<MatchBase, string, MatchBaseFilterRequest>, IEndpoint
    {



        [Get("/GetByType/{MatchBaseType}")]
        Task<SerializableResult<PagedCursorResponse<MatchBase>>> GetByMatchBaseType([FromRoute] MatchBaseType MatchBaseType, [AsParameters] PagedCursorRequest<string> PagedRequest);

        [Put("/{MatchBaseType}")]
        Task<Result> UpdateMatchBaseScore([FromRoute] MatchBaseType MatchBaseType, [FromQuery] string MatchHash, [FromQuery] decimal NewScore);

        [Post("/AddContext")]
        Task<Result> AddMatchBaseContext(string MatchHash, [FromBody] AddMatchContext MatchContext);

        [Post("/RemoveContext")]
        Task<Result> RemoveMatchBaseContext(string MatchHash, [FromBody] RemoveMatchContext MatchContext);

        [Put("/ReorderContext")]
        Task<Result> ReorderMatchBaseContext(string MatchHash, [FromBody] string[] MatchContextsOrder);

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
