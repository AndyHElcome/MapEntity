using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Models.Attributes;
using MongoDB.Bson;
using Version = MMIv8_Ktype.Models.Collections.Version;
using Refit;
using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Models.Collections;

namespace MMIv8_Ktype.Api.Endpoints
{
    [GroupName("Version")]
    public interface IVersionEndpoints : IEndpoint
    {
        [Get("")]
        Task<List<Version>> GetAll();

        [Get("/{VersionNumber}")]
        Task<Version?> GetByVersion(int VersionNumber);

        [Get("/CurrentVersionID")]
        Task<ObjectId?> GetCurrentVersionID();

        [Get("/CurrentVersion")]
        Task<Version?> GetCurrentVersion();

        [Put("/Create")]
        Task<Version> CreateVersion(CreateVersionRequest request);

        [Patch("/{VersionNumber}")]
        Task<Version?> UpdateVersion([FromHeader] int VersionNumber, [FromQuery] string? TecdocEntityVersion = null, [FromQuery] string? MMIv8EntityVersion = null, [FromQuery] string? UserName = null);

        [Delete("/Debug/DeleteAll")]
        Task DeleteAll();
    }
}
