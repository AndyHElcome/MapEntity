using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMIv8_Ktype.Api.Requests
{
    public record MatchEntityByExternalRequest(int KtypNr, int MMI_V8_Key) : IRequest;
    public record UpdateFlagRequest(int KTypNr, int MMI_V8_Key, bool Flag, string? Detail = null) : IRequest;
}
