using MMIv8_Ktype.Api.Requests;
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
        Task<List<MatchBase>> GetAll();

        [Get("/GetByType/{MatchBaseType}")]
        Task<List<MatchBase>> GetByMatchBaseType(MatchBaseType MatchBaseType);

        [Get("/GetById/{MatchHash}")]
        Task<MatchBase?> GetById(string MatchHash);

        [Put("")]
        Task UpdateMatchBaseScore(PutMatchBaseRequest request);

        [Put("/Partial/")]
        Task StorePartialMatchBase(PutMatchBaseRequest request);

        [Delete("/Partial/{MatchBaseType}/{MatchHash}")]
        Task RemovePartialMatchBase(MatchBaseType MatchBaseType, string MatchHash);
    }
}
