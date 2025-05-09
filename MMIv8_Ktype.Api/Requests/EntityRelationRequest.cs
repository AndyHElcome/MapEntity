using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMIv8_Ktype.Api.Requests
{
    public record PutEntityRelationRequest(int MMI_V8_Key, int KTypNr, int VersionNumber, string? Comment = null);
}
