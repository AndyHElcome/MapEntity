using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Models.Attributes;
using MMIv8_Ktype.Models.Collections;
using MongoDB.Bson;
using Refit;

namespace MMIv8_Ktype.Api.Endpoints
{
    [GroupName("MatchMakeModel")]
    public interface IMatchMakeModelEndpoints : IEndpoint
    {
        [Get("")]
        Task<List<MatchMakeModel>> GetAllMakeModelMatch();

        [Get("/{MatchID}")]
        Task<MatchMakeModel?> GetMakeModelMatchById(ObjectId MatchID);

        [Post("/GetByModels")]
        Task<MatchMakeModel?> GetMakeModelMatch(MatchMakeModelRequest request);

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
