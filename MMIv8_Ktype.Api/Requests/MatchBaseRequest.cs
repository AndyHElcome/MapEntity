using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Models.Util;

namespace MMIv8_Ktype.Api.Requests
{
    public record PutMatchBaseRequest(MatchBaseType MatchBaseType, string MatchHash, decimal NewScore) : IRequest;
    public record DeleteMatchBaseRequest(MatchBaseType MatchBaseType, string MatchHash) : IRequest;
}
