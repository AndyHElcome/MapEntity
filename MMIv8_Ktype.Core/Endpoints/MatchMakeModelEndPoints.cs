using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Requests;
using MongoDB.Bson;

namespace MMIv8_Ktype.Core.Endpoints
{
    public class MatchMakeModelEndPoints : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder routeBuilder)
        {
            var group = routeBuilder.MapGroup("MatchMakeModel").WithTags("MatchMakeModel");

            group.MapPost(nameof(CreateMakeModelMatch), CreateMakeModelMatch).WithName(nameof(CreateMakeModelMatch));
            group.MapPost(nameof(GetMakeModelMatch), GetMakeModelMatch).WithName(nameof(GetMakeModelMatch));
            group.MapPost(nameof(GetMakeModelMatchById) + "/{MatchID}", GetMakeModelMatchById).WithName(nameof(GetMakeModelMatchById));
            group.MapPost(nameof(DeleteMakeModelMatch) + "/{MatchID}", DeleteMakeModelMatch).WithName(nameof(DeleteMakeModelMatch));
            group.MapPost(nameof(GenerateModelMatch), GenerateModelMatch).WithName(nameof(GenerateModelMatch));
        }

        public static async Task<IResult> CreateMakeModelMatch(MakeModelMatchRequest request, MappingService mappingService) //Can change to TypeResults for Swagger
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

        public static async Task<IResult> GetMakeModelMatch(MakeModelMatchRequest request, MatchMakeModelService matchMakeModelService)
        {
            var response = await matchMakeModelService.GetByModelIds(request);

            if (response is not null)
            {
                return Results.Ok(response);
            }
            else
            {
                return Results.NotFound();
            }
        }

        public static async Task<IResult> GetMakeModelMatchById(ObjectId MatchID, MatchMakeModelService matchMakeModelService)
        {
            var response = await matchMakeModelService.GetById(MatchID);

            if (response is not null)
            {
                return Results.Ok<MatchMakeModel>(response);
            }
            else
            {
                return Results.NotFound();
            }
        }

        public static async Task<IResult> DeleteMakeModelMatch(ObjectId MatchID, MappingService mappingService)
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

        public static async Task<IResult> GenerateModelMatch(BulkMappingService bulkMappingService)
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
}
