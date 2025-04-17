using MMIv8_Ktype.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMIv8_Ktype.Models.Requests
{
    public record CreateVersionRequest(string TecDocEntityVersion, string MMIv8EntityVersion, string User) : IRequest;
}
