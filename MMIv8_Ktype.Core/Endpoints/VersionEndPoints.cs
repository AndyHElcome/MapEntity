using MMIv8_Ktype.Core.Services;
using MMIv8_Ktype.Api.Endpoints;
using MongoDB.Driver;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Core.Services.Mapping;
using Version = MMIv8_Ktype.Models.Collections.Version;
using MongoDB.Bson;
using MMIv8_Ktype.Models.Collections;
using Serilog;
using MMIv8_Ktype.Core.Services.Match;
using Microsoft.AspNetCore.Http.HttpResults;
using MMIv8_Ktype.Models;

namespace MMIv8_Ktype.Core.Endpoints
{
    public class VersionEndPoints(VersionService versionService, MappingService mappingService) : IVersionEndpoints
    {
        public async Task<SerializableResult<List<Version>>> GetAll() //TODO change results to stream
        {
            return Result.Success(await versionService.GetFindFluent().ToListAsync());
        }

        public async Task<SerializableResult<Version>> GetCurrentVersion()
        {
            return await versionService.GetCurrentVersion();
        }

        public async Task<SerializableResult<Version>> GetByVersion(int VersionNumber)
        {
            return await versionService.GetByVersion(VersionNumber);
        }

        public async Task<SerializableResult<Version>> CreateVersion(string TecDocEntityVersion, string MMIv8EntityVersion, string UserName)
        {
            return await mappingService.CreateVersion(TecDocEntityVersion, MMIv8EntityVersion, UserName);
        }

        public async Task<SerializableResult<Version>> UpdateVersion(int VersionNumber, string? TecdocEntityVersion = null, string? MMIv8EntityVersion = null, string? UserName = null)
        {
            if (TecdocEntityVersion is null && MMIv8EntityVersion is null && UserName is null)
            {
                Log.Error("No updates given for Version {versionNumber}", VersionNumber);
                return Result<Version>.Failure<Version>(Error.Validation("Version.UpdateValidation", $"No updates given for Version {VersionNumber}"));
            }

            return await mappingService.UpdateVersion(VersionNumber, TecdocEntityVersion, MMIv8EntityVersion, UserName);
        }

        public async Task<SerializableResult<DeleteResult>> DeleteAll()
        {
            return await versionService.DeleteAll();
        }
    }
}
