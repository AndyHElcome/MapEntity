using Microsoft.AspNetCore.Builder;
using MMIv8_Ktype.Api;
using MMIv8_Ktype.Core.Services;
using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Endpoints;
using MMIv8_Ktype.Models.Requests;
using MongoDB.Bson;

namespace MMIv8_Ktype.Core.Endpoints
{
    public class VersionEndPoints
    {
        public void MapEndpoint(IEndpointRouteBuilder routeBuilder)
        {
            var group = routeBuilder.MapGroup("Version").WithTags("Version");

            group.MapPost(nameof(GetAll), GetAll);
            group.MapPost(nameof(GetCurrentVersion), GetCurrentVersion);
            group.MapPost(nameof(GetByVersion) + "/{VersionNumber}", GetByVersion);
            group.MapPost(nameof(CreateVersion), CreateVersion);
        }

        public static async Task<IResult> GetAll(VersionService versionService) //TODO change results to stream
        {
            var response = await versionService.GetAll();

            if (response is not null)
            {
                return Results.Ok(response);
            }
            else
            {
                return Results.InternalServerError();
            }
        }

        public static async Task<IResult> GetCurrentVersion(VersionService versionService)
        {
            var response = await versionService.GetCurrentVersion();

            if (response is not null)
            {
                return Results.Ok(response);
            }
            else
            {
                return Results.InternalServerError();
            }
        }

        public static async Task<IResult> GetByVersion(int VersionNumber, VersionService versionService)
        {
            var response = await versionService.GetByVersion(VersionNumber);

            if (response is not null)
            {
                return Results.Ok(response);
            }
            else
            {
                return Results.NotFound();
            }
        }

        public static async Task<IResult> CreateVersion(CreateVersionRequest request, VersionService versionService)
        {
            var response = await versionService.Create(request);

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
