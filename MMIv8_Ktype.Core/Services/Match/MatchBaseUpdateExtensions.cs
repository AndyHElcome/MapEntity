using MMIv8_Ktype.Models.Collections;
using MongoDB.Driver;

namespace MMIv8_Ktype.Core.Services.Match
{
    public static class MatchBaseUpdateExtensions
    {
        public static UpdateDefinition<MatchBase> UpdateMatchBaseScore(this UpdateDefinition<MatchBase> update, decimal newScore)
        {
            update = update.Set(c => c.Score, newScore);
            return update;
        }

        public static UpdateDefinition<MatchBase> SetMatchContext(this UpdateDefinition<MatchBase> update, List<MatchContext>? matchContext)
        {
            update = update.Set(c => c.MatchContexts, matchContext);
            return update;
        }

    }
}
