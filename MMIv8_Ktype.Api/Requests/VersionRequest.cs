using MongoDB.Driver;
using Version = MMIv8_Ktype.Models.Collections.Version;

namespace MMIv8_Ktype.Api.Requests
{
    public record CreateVersionRequest(string TecDocEntityVersion, string MMIv8EntityVersion, string UserName) : IRequest;

    public class VersionSortRequest : SortQuery<Version>
    {
        public new SortDefinition<Version>? GetSort()
        {
            return new SortDefinitionBuilder<Version>().Ascending(c => c.VersionNumber);
        }
    }

    public class VersionFilterRequest : FilterQuery<Version>
    {
        public string? TecDocEntityVersion { get; set; } = null;
        public string? MMIv8EntityVersion { get; set; } = null;
        public string[]? UserName { get; set; } = null;

        public new FilterDefinition<Version> GetFilter()
        {
            var filterBuilder = Builders<Version>.Filter;
            var filter = filterBuilder.Empty;

            if (TecDocEntityVersion is not null)
                filter = filter & filterBuilder.Eq(c => c.TecDocEntityVersion, TecDocEntityVersion);

            if (MMIv8EntityVersion is not null)
                filter = filter & filterBuilder.Eq(c => c.MMIv8EntityVersion, MMIv8EntityVersion);

            if (UserName is not null && UserName.Length > 0)
                filter = filter & filterBuilder.In(c => c.User.Name, UserName);

            return filter;
        }
    }

}
