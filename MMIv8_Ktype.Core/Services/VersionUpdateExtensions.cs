using MMIv8_Ktype.Models.Collections;
using MongoDB.Driver;

namespace MMIv8_Ktype.Core.Services
{
    public static class VersionUpdateExtensions
    {
        public static UpdateDefinition<Models.Collections.Version> UpdateVersionTecdocEntityVersion(this UpdateDefinition<Models.Collections.Version> update, string? tecdocEntityVersion = null)
        {
            if (tecdocEntityVersion is not null)
                update = update.Set(c => c.TecDocEntityVersion, tecdocEntityVersion);

            return update;
        }
        public static UpdateDefinition<Models.Collections.Version> UpdateVersionMMIv8EntityVersion(this UpdateDefinition<Models.Collections.Version> update, string? mmiv8EntityVersion = null)
        {
            if (mmiv8EntityVersion is not null)
                update = update.Set(c => c.MMIv8EntityVersion, mmiv8EntityVersion);

            return update;
        }
        public static UpdateDefinition<Models.Collections.Version> UpdateVersionUser(this UpdateDefinition<Models.Collections.Version> update, User? user = null)
        {
            if (user is not null)
                update = update.Set(c => c.User, user);

            return update;
        }
    }
}
