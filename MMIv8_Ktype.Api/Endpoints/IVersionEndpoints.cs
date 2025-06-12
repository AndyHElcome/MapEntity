using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Models.Attributes;
using MongoDB.Bson;
using Version = MMIv8_Ktype.Models.Collections.Version;
using Refit;
using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Models.Collections;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Threading.Tasks;
using MongoDB.Driver;
using MMIv8_Ktype.Models;

namespace MMIv8_Ktype.Api.Endpoints
{
    [GroupName("Version")]
    public interface IVersionEndpoints : IEndpoint
    {
        [Get("")]
        Task<SerializableResult<List<Version>>> GetAll();

        [Get("/{VersionNumber}")]
        Task<SerializableResult<Version>> GetByVersion(int VersionNumber);

        [Get("/CurrentVersion")]
        Task<SerializableResult<Version>> GetCurrentVersion();

        [Put("/Create")]
        Task<SerializableResult<Version>> CreateVersion(string TecDocEntityVersion, string MMIv8EntityVersion, string UserName);

        [Patch("/{VersionNumber}")]
        Task<SerializableResult<Version>> UpdateVersion([FromHeader] int VersionNumber, [FromQuery] string? TecdocEntityVersion = null, [FromQuery] string? MMIv8EntityVersion = null, [FromQuery] string? UserName = null);

        [Delete("/Debug/DeleteAll")]
        Task<Result> DeleteAll();
    }
}
