// minimal endpoint https://youtu.be/gsAuFIhXz3g?si=MfaGxzKFgLlgWIbR
// reflection endpoint mapping https://youtu.be/CkGFV5bekbY?si=GkVIYuPIObrZDMu1
namespace MMIv8_Ktype.Api.Requests
{
    public record MatchMakeModelRequest(string TD_SourceEntityModelHash, string MMI_SourceEntityModelHash) : IRequest;
    public class PutMatchMakeModelRequest : IRequest
    {
        public string TD_SourceEntityModelHash { get; set; } = "";
        public string MMI_SourceEntityModelHash { get; set; } = "";
    }
}
