using MMIv8_Ktype.Models.Util;
using MongoDB.Driver;
using Serilog;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Core.Contexts;
using MongoDB.Bson;

namespace MMIv8_Ktype.Core.Services.Match
{
    public class MatchBaseService(MongoDBContext MMIv8_Ktype, IVersionProvider versionProvider) : BaseServiceWithVersion<MatchBase, string>(MMIv8_Ktype.Collections.MatchBase, versionProvider)
    {
        public async Task<IAsyncCursor<MatchBase>> GetByType(MatchBaseType matchBaseType, FilterDefinition<MatchBase>? filter = null, int? batchSize = null) //TODO Rename or remove
        {
            var builder = Builders<MatchBase>.Filter;
            filter ??= builder.Empty;
            filter &= builder.Eq(c => c.MatchBaseType, matchBaseType);
            return await base.GetCursor(filter: filter, batchSize: batchSize);
        }

        public async Task<IAsyncCursor<MatchBase>> GetByTypeAndMethod(MatchBaseType matchBaseType, MatchBaseMethod method, FilterDefinition<MatchBase>? filter = null, int? batchSize = null) //TODO Rename or remove
        {
            var builder = Builders<MatchBase>.Filter;
            filter ??= builder.Empty;
            filter &= builder.Eq(c => c.MatchBaseType, matchBaseType)
                    & builder.Eq(c => c.MatchBaseMethod, method);
            return await base.GetCursor(filter: filter, batchSize: batchSize);
        }

        public CombinationPipeline<MatchBase> UpdateScore(MatchBase matchBase, decimal newScore)
        {
            var filter = Builders<MatchBase>.Filter.Eq(c => c.DocumentId, matchBase.DocumentId);

            return base.Update(filter).AppendUpdate(c => c.UpdateMatchBaseScore(newScore)) //TODO Should I move this into the query or leave it here as always necessary?
                                      .AppendPipeline(c => c.AppendStatus(VersionProvider.NewStatus(Status.Updated, $"Score: {newScore.ToString()}")));
        }
    }
}
