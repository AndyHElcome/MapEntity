using MMIv8_Ktype.Core.Services;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Status;

namespace MMIv8_Ktype.Core.Contexts
{
    public class VersionProviderMongo(VersionService versionService) : IVersionProvider
    {
        public Models.Collections.Version Version => versionService.GetCurrentVersion().Result ?? new();

        public StatusChange NewStatus(Status status, string? detail = null) => new()
        {
            Status = status,
            DateOfChange = DateTime.Now,
            Version = Version,
            Detail = detail
        };
    }
}
