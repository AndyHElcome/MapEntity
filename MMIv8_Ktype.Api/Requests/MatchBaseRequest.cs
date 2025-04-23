using MMIv8_Ktype.Models.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMIv8_Ktype.Api.Requests
{
    public record PutMatchBaseRequest(MatchBaseType MatchBaseType, string MatchHash, decimal NewScore) : IRequest;
    public record DeleteMatchBaseRequest(MatchBaseType MatchBaseType, string MatchHash) : IRequest;
}
