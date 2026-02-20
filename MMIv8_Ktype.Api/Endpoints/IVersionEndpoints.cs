using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Attributes;
using MongoDB.Bson;
using Refit;
using Version = MMIv8_Ktype.Models.Collections.Version;

namespace MMIv8_Ktype.Api.Endpoints
{
    [GroupName("Version")]
    public interface IVersionEndpoints : IBaseEndpoint<Version, ObjectId, VersionFilterRequest, VersionSortRequest>, IEndpoint
    {
        [Get("/{VersionNumber}")]
        Task<SerializableResult<Version>> GetByVersion(int VersionNumber);

        [Get("/CurrentVersion")]
        Task<SerializableResult<Version>> GetCurrentVersion();

        [Put("/Create")]
        Task<SerializableResult<Version>> CreateVersion(string TecDocEntityVersion, string MMIv8EntityVersion, string UserName);

        [Put("/{VersionNumber}")]
        Task<SerializableResult<Version>> UpdateVersion([FromHeader] int VersionNumber, [FromQuery] string? TecdocEntityVersion = null, [FromQuery] string? MMIv8EntityVersion = null, [FromQuery] string? UserName = null);

        [Delete("/Debug/DeleteAll")]
        Task<Result> DeleteAll();
    }
}
