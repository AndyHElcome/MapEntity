// minimal endpoint https://youtu.be/gsAuFIhXz3g?si=MfaGxzKFgLlgWIbR
// reflection endpoint mapping https://youtu.be/CkGFV5bekbY?si=GkVIYuPIObrZDMu1
using MMIv8_Ktype.Models.Collections;
using MongoDB.Driver;

namespace MMIv8_Ktype.Api.Requests
{

    public class MatchMakeModelSortRequest : SortQuery<MatchMakeModel>
    {
        public override SortDefinition<MatchMakeModel> GetSort()
        {
            return Builders<MatchMakeModel>.Sort.Ascending(c => c.TextSort);
        }
    }
    public record MatchMakeModelRequest(string TD_SourceEntityModelHash, string MMI_SourceEntityModelHash) : IRequest;
    public class PutMatchMakeModelRequest : IRequest
    {
        public string TD_SourceEntityModelHash { get; set; } = "";
        public string MMI_SourceEntityModelHash { get; set; } = "";
    }
}
