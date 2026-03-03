using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Indexes;
using MongoDB.Driver;

namespace MMIv8_Ktype.Core.Services.Mapping
{
    public static class SourceEntityUpdateExtensions
    {
        public static UpdateDefinition<TEntity> UpdateMatchRefine<TEntity>(this UpdateDefinition<TEntity> update, MatchRefine matchRefine)
            where TEntity : SourceEntity
        {
            update = update.Set(c => c.MatchRefine, matchRefine);
            return update;
        }
    }
}
