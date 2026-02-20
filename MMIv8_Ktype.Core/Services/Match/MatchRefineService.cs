using Microsoft.Extensions.Options;
using MMIv8_Ktype.Api;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Indexes;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Serilog;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection.Metadata;

namespace MMIv8_Ktype.Core.Services.Match
{

    public class MatchRefineService(MongoDBContext MMIv8_Ktype) : BaseService<MatchRefine, ObjectId>(MMIv8_Ktype.Collections.MatchRefine)
    {
       
    }
}
