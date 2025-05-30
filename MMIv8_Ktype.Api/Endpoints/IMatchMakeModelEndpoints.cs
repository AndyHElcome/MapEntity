using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Models.Attributes;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Indexes;
using MongoDB.Bson;
using Refit;

namespace MMIv8_Ktype.Api.Endpoints
{
    [GroupName("MatchMakeModel")]
    public interface IMatchMakeModelEndpoints : IEndpoint
    {
        [Get("")]
        Task<PagedCursorResponse<MatchMakeModel>> GetAll([FromQuery] string? cursor = null, [FromQuery] int PageSize = 0);

        [Get("/{MatchID}")]
        Task<MatchMakeModel?> GetMakeModelMatchById(ObjectId MatchID);

        [Get("/GetByModels")]
        Task<MatchMakeModel?> GetMakeModelMatch([FromQuery] string TD_SourceEntityModelHash, [FromQuery] string MMI_SourceEntityModelHash);

        [Get("/{SourceIndex}/{SourceEntityModelHash}")]
        Task<List<MatchMakeModel>> GetByModelId(SourceIndex SourceIndex, string SourceEntityModelHash);

        [Post("/GenerateModelMatch")]
        Task<List<MatchMakeModel>> GenerateMakeModelMatch();

        [Put("/Create")]
        Task CreateMakeModelMatch(MatchMakeModelRequest request);

        [Delete("/{MatchID}")]
        Task DeleteMakeModelMatch(ObjectId MatchID);

        [Delete("/Debug/DeleteAll")]
        Task DeleteAll();
    }
}
