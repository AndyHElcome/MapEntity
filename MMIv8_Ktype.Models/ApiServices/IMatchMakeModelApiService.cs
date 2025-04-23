using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MMIv8_Ktype.Api;
using MMIv8_Ktype.Models.Abstractions;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Endpoints;
using MMIv8_Ktype.Models.Requests;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MMIv8_Ktype.Models.ApiServices
{
    [GroupName("MatchMakeModel")]
    public interface IMatchMakeModelApiService : IEndpoint  //FOR INTERFACE
    {
        [Post("/CreateMakeModelMatch")]
        Task<IResult> CreateMakeModelMatch(MakeModelMatchRequest request);

        [Post("/GetMakeModelMatch")]
        Task<MatchMakeModel?> GetMakeModelMatch(MakeModelMatchRequest request);

        [Post("/GetMakeModelMatchById")]
        Task<MatchMakeModel?> GetMakeModelMatchById(ObjectId MatchID);

        [Post("/DeleteMakeModelMatch")]
        Task<IResult> DeleteMakeModelMatch(ObjectId MatchID);

        [Post("/GenerateModelMatch")]
        Task<IResult> GenerateModelMatch();
    }



    [AttributeUsage(AttributeTargets.Interface)]
    public class GroupName(string name) : Attribute
    {
        public string Name { get; } = name;
    }

    

    //public abstract class MatchMakeModelApiService : IEndpoint
    //{
    //    public static string _GroupName => "MatchMakeModel";
    //    public string GroupName => _GroupName;

    //    [ Post("/CreateMakeModelMatch")]
    //    public abstract  Task<IResult> CreateMakeModelMatch(MakeModelMatchRequest request);

    //    [Post("/CreateMakeModelMatch/GetMakeModelMatch")]
    //    public abstract Task<IResult> GetMakeModelMatch(MakeModelMatchRequest request);

    //    [Post("/CreateMakeModelMatch/GetMakeModelMatch2")]
    //    public abstract Task<IResult> GetMakeModelMatchById(ObjectId MatchID);

    //    [Post("/CreateMakeModelMatch/GetMakeModelMatch3")]
    //    public abstract Task<IResult> DeleteMakeModelMatch(ObjectId MatchID);

    //    [Post("/CreateMakeModelMatch/GetMakeModelMatch4")]
    //    public abstract Task<IResult> GenerateModelMatch();
    //}
}
