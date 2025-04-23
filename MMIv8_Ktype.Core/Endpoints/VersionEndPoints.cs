using MMIv8_Ktype.Core.Services;
using MMIv8_Ktype.Api.Endpoints;
using MongoDB.Driver;
using MMIv8_Ktype.Api.Requests;

namespace MMIv8_Ktype.Core.Endpoints
{
    public class VersionEndPoints(VersionService versionService) : IVersionEndpoints
    {
        public async Task<List<Models.Collections.Version>> GetAll() //TODO change results to stream
        {
            var response = await versionService.GetAll();

            return await response.ToListAsync();
        }

        public async Task<Models.Collections.Version?> GetCurrentVersion()
        {
            return await versionService.GetCurrentVersion();
        }

        public async Task<Models.Collections.Version?> GetByVersion(int VersionNumber)
        {
            return await versionService.GetByVersion(VersionNumber);
        }

        public async Task<Models.Collections.Version?> CreateVersion(CreateVersionRequest request)
        {
            return await versionService.Create(request);
        }
    }
}
