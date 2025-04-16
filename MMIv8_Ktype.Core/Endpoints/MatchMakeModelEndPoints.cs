using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Models.Collections;
using MongoDB.Bson;

namespace MMIv8_Ktype.Core.Endpoints
{
    public class MatchMakeModelEndPoints : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder routeBuilder)
        {
            var group = routeBuilder.MapGroup("api/MatchMakeModel");

            group.MapPost("", CreateMakeModelMatch);
            group.MapPost("", GetMakeModelMatch).WithName(nameof(GetMakeModelMatch));
            group.MapGet("{MatchID}", GetMakeModelMatchById).WithName(nameof(GetMakeModelMatchById));
            group.MapDelete("{MatchID}", DeleteMakeModelMatch).WithName(nameof(DeleteMakeModelMatch));
        }

        public static async Task<IResult> CreateMakeModelMatch(ImportMatchMakeModel request, MappingService mappingService) //Can change to TypeResults for Swagger
        {
            var response = await mappingService.CreateMakeModelMatch(request);

            return Results.Created($"api/matchmakemodel/{response}", response);
        }

        public static async Task<IResult> GetMakeModelMatch(ImportMatchMakeModel request, MatchMakeModelService matchMakeModelService)
        {
            var response = await matchMakeModelService.GetByModelIds(request);

            if (response is null)
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

            if (response is null)
            {
                return Results.Ok(response);
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
                return Results.NoContent();
            }
            else
            {
                return Results.NotFound();
            }
        }


    }
}
