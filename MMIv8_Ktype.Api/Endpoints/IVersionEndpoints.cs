using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Models.Attributes;
using Refit;

namespace MMIv8_Ktype.Api.Endpoints
{
    [GroupName("Version")]
    public interface IVersionEndpoints : IEndpoint
    {
        [Get("")]
        Task<List<Models.Collections.Version>> GetAll();

        [Get("/{VersionNumber}")]
        Task<Models.Collections.Version?> GetByVersion(int VersionNumber);

        [Get("/CurrentVersion")]
        Task<Models.Collections.Version?> GetCurrentVersion();

        [Put("/Create")]
        Task<Models.Collections.Version?> CreateVersion(CreateVersionRequest request);
    }
}
