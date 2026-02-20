using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MMIv8_Ktype.Api;
using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Core.Services;
using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Outputs;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Reflection.Metadata;
using System.Security.Cryptography;

namespace MMIv8_Ktype.Core.Endpoints
{
    public class MatchRefineEndpoints(MatchRefineService matchEntityService, MappingService mappingService) : BaseEndpoints<MatchRefine, ObjectId, FilterQuery<MatchRefine>, SortQuery<MatchRefine>>(matchEntityService), IMatchRefineEndpoints
    {

    }
}
