using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Attributes;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Indexes;
using MongoDB.Bson;
using MongoDB.Driver;
using Refit;

namespace MMIv8_Ktype.Api.Endpoints
{
    [GroupName("MatchMakeModel")]
    public interface IMatchMakeModelEndpoints : IBaseEndpoint<MatchMakeModel, ObjectId, FilterQuery<MatchMakeModel>, MatchMakeModelSortRequest>, IVersionEndpoint<MatchMakeModel, ObjectId, FilterQuery<MatchMakeModel>>, IEndpoint
    {

        [Get("/GetByModels")]
        Task<SerializableResult<MatchMakeModel>> GetMakeModelMatch([FromQuery] string TD_SourceEntityModelHash, [FromQuery] string MMI_SourceEntityModelHash);

        [Get("/{SourceIndex}/{SourceEntityModelHash}")]
        Task<SerializableResult<List<MatchMakeModel>>> GetByModelId(SourceIndex SourceIndex, string SourceEntityModelHash);

        [Post("/GenerateModelMatch")]
        Task<SerializableResult<List<MatchMakeModel>>> GenerateMakeModelMatch();

        [Put("/Create")]
        Task<SerializableResult<MatchMakeModel>> CreateMatchMakeModel(string TD_SourceEntityModelHash = "", string MMI_SourceEntityModelHash = "");

        [Delete("/{MatchID}")]
        Task<Result> DeleteMakeModelMatch(ObjectId MatchID);

        [Post("/Debug/RegenerateMakeModelSort")]
        Task RegenerateMakeModelSort();

        [Delete("/Debug/DeleteAll")]
        Task<Result> DeleteAll();
    }
}
