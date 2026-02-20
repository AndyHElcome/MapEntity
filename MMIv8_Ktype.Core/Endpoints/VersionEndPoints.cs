using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Core.Services;
using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Status;
using MongoDB.Bson;
using MongoDB.Driver;
using Refit;
using Serilog;
using System.Security.Cryptography;
using Version = MMIv8_Ktype.Models.Collections.Version;

namespace MMIv8_Ktype.Core.Endpoints
{

    public class VersionEndPoints(VersionService versionService, MappingService mappingService) : BaseEndpoints<Version, ObjectId, VersionFilterRequest, VersionSortRequest>(versionService), IVersionEndpoints
    {

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
                return Error.Validation("Version.UpdateValidation", $"No updates given for Version {VersionNumber}");
            }

            return await mappingService.UpdateVersion(VersionNumber, TecdocEntityVersion, MMIv8EntityVersion, UserName);
        }

        public async Task<Result> DeleteAll()
        {
            return await versionService.DeleteAll();
        }
    }
}
