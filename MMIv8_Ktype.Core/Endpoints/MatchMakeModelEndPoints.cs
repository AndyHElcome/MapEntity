using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Api;
using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Models.Abstractions;
using MMIv8_Ktype.Models.ApiServices;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Endpoints;
using MMIv8_Ktype.Models.Requests;
using MongoDB.Bson;
using Refit;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace MMIv8_Ktype.Core.Endpoints
{
    public class RefitHttpClientFactory<T> 
    {
        private readonly IHttpClientFactory _clientFactory;

        public RefitHttpClientFactory(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public T CreateClient(string baseAddressKey, IEndpoint endpoint)
        {
            var client = _clientFactory.CreateClient(baseAddressKey + endpoint);

            return RestService.For<T>(client);
        }
    }

    public class MatchMakeModelEndPoints(MappingService mappingService,
                                         MatchMakeModelService matchMakeModelService,
                                         BulkMappingService bulkMappingService) : IMatchMakeModelApiService  //FOR INTERFACE
    {
       
        public async Task<IResult> CreateMakeModelMatch(MakeModelMatchRequest request) //Can change to TypeResults for Swagger
        {
            try
            {
                var response = await mappingService.CreateMakeModelMatch(request);

                return Results.Created($"api/matchmakemodel/{response}", response);
            }
            catch
            {
                return Results.InternalServerError();
            }
        }

        public async Task<MatchMakeModel?> GetMakeModelMatch(MakeModelMatchRequest request)
        {
            var response = await matchMakeModelService.GetByModelIds(request);

            return response;

        }

        //public async Task<IResult> GetMakeModelMatch(MakeModelMatchRequest request)
        //{
        //    var response = await matchMakeModelService.GetByModelIds(request);

        //    if (response is not null)
        //    {
        //        return Results.Ok(response);
        //    }
        //    else
        //    {
        //        return Results.NotFound();
        //    }
        //}

        public async Task<MatchMakeModel?> GetMakeModelMatchById(ObjectId MatchID)
        {
            var response = await matchMakeModelService.GetById(MatchID);


            return response;
        }

        public async Task<IResult> DeleteMakeModelMatch(ObjectId MatchID)
        {
            var response = await mappingService.DeleteMakeModelMatch(MatchID);

            if (response is not null)
            {
                return Results.Ok(response);
            }
            else
            {
                return Results.NotFound();
            }
        }

        public async Task<IResult> GenerateModelMatch()
        {
            var response = await bulkMappingService.GenerateModelMatch();

            if (response is not null)
            {
                return Results.Ok(response);
            }
            else
            {
                return Results.NotFound();
            }
        }

    }

    //public class MatchMakeModelEndPoints(MappingService mappingService,
    //                                     MatchMakeModelService matchMakeModelService,
    //                                     BulkMappingService bulkMappingService) : MatchMakeModelApiService
    //{
    //    public override async Task<IResult> CreateMakeModelMatch(MakeModelMatchRequest request) //Can change to TypeResults for Swagger
    //    {
    //        try
    //        {
    //            var response = await mappingService.CreateMakeModelMatch(request);

    //            return Results.Created($"api/matchmakemodel/{response}", response);
    //        }
    //        catch
    //        {
    //            return Results.InternalServerError();
    //        }
    //    }

    //    public override async Task<IResult> GetMakeModelMatch(MakeModelMatchRequest request)
    //    {
    //        var response = await matchMakeModelService.GetByModelIds(request);

    //        if (response is not null)
    //        {
    //            return Results.Ok(response);
    //        }
    //        else
    //        {
    //            return Results.NotFound();
    //        }
    //    }

    //    public override async Task<IResult> GetMakeModelMatchById(ObjectId MatchID)
    //    {
    //        var response = await matchMakeModelService.GetById(MatchID);

    //        if (response is not null)
    //        {
    //            return Results.Ok<MatchMakeModel>(response);
    //        }
    //        else
    //        {
    //            return Results.NotFound();
    //        }
    //    }

    //    public override async Task<IResult> DeleteMakeModelMatch(ObjectId MatchID)
    //    {
    //        var response = await mappingService.DeleteMakeModelMatch(MatchID);

    //        if (response is not null)
    //        {
    //            return Results.Ok(response);
    //        }
    //        else
    //        {
    //            return Results.NotFound();
    //        }
    //    }

    //    public override async Task<IResult> GenerateModelMatch()
    //    {
    //        var response = await bulkMappingService.GenerateModelMatch();

    //        if (response is not null)
    //        {
    //            return Results.Ok(response);
    //        }
    //        else
    //        {
    //            return Results.NotFound();
    //        }
    //    }

    //}
}
