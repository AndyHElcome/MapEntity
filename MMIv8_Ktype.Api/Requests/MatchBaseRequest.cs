using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Models.Util;

namespace MMIv8_Ktype.Api.Requests
{
    public record PutMatchBaseRequest([FromQuery] MatchBaseType MatchBaseType, [FromQuery] string MatchHash, [FromQuery] decimal NewScore) : IRequest;
    public record DeleteMatchBaseRequest([FromQuery] MatchBaseType MatchBaseType, [FromQuery] string MatchHash) : IRequest;
}
