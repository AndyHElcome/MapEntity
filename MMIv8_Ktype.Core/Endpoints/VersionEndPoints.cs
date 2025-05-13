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

namespace MMIv8_Ktype.Core.Endpoints
{
    public class VersionEndPoints(VersionService versionService, MappingService mappingService) : IVersionEndpoints
    {
        public async Task<List<Version>> GetAll() //TODO change results to stream
        {
            return await versionService.GetFindFluent().ToListAsync();
        }

        public async Task<ObjectId?> GetCurrentVersionID()
        {
            var version = await versionService.GetCurrentVersion();
            return version?.DocumentId;
        }

        public async Task<Version?> GetCurrentVersion()
        {
            return await versionService.GetCurrentVersion();
        }

        public async Task<Version?> GetByVersion(int VersionNumber)
        {
            return await versionService.GetByVersion(VersionNumber);
        }

        public async Task<Version> CreateVersion(CreateVersionRequest request)
        {
            return await mappingService.CreateVersion(request);
        }

        public async Task<Version?> UpdateVersion(int VersionNumber, string? TecdocEntityVersion = null, string? MMIv8EntityVersion = null, string? UserName = null)
        {
            if (TecdocEntityVersion is null && MMIv8EntityVersion is null && UserName is null)
            {
                Log.Error("No updates given for Version {versionNumber}", VersionNumber);
                //throw new Exception("");
                return null;
            }

            return await mappingService.UpdateVersion(VersionNumber, TecdocEntityVersion, MMIv8EntityVersion, UserName);
        }

        public async Task DeleteAll()
        {
            await versionService.DeleteAll();
        }
    }
}
