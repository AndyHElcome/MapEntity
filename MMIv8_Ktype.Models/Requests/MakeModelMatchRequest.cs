// minimal endpoint https://youtu.be/gsAuFIhXz3g?si=MfaGxzKFgLlgWIbR
// reflection endpoint mapping https://youtu.be/CkGFV5bekbY?si=GkVIYuPIObrZDMu1
using MMIv8_Ktype.Api;

namespace MMIv8_Ktype.Models.Requests
{
    public record MakeModelMatchRequest(string TD_SourceEntityModelHash, string MMI_SourceEntityModelHash) : IRequest;
}
