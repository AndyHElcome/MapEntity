using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Attributes;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Outputs;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using MongoDB.Driver;
using Refit;

namespace MMIv8_Ktype.Api.Endpoints
{
    [GroupName("MatchRefine")]
    public interface IMatchRefineEndpoints : IBaseEndpoint<MatchRefine, ObjectId, FilterQuery<MatchRefine>, SortQuery<MatchRefine>>, IEndpoint
    {
        
    }
}
