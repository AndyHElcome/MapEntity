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
        public async Task<Result<List<MatchBase>>> GetByMatchBaseType(MatchBaseType matchBaseType, FilterDefinition<MatchBase>? filter = null) //TODO could be endpoint but then how does mapping work?
        {
            try
            {
                var builder = Builders<MatchBase>.Filter;
                filter ??= builder.Empty;
                filter = builder.Eq(c => c.MatchBaseType, matchBaseType);
                var result = await base.GetFindFluent(filter).ToListAsync();

                return result is { Count: > 0 } ? result : Error.NoContent("MatchBase.NoContentByMatchBaseType", $"No MatchBase records exist for type {matchBaseType.ToString()}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving MatchBases {@matchBaseType}", matchBaseType.ToString());
                return Error.Failure($"MatchBase.GetByMatchBaseTypeFailure", $"Error getting Documents with type {matchBaseType.ToString()}. Error: {ex.Message}");
            }
        }

        public async Task<Result<List<MatchBase>>> GetByTypeAndMethod(MatchBaseType matchBaseType, MatchBaseMethod method, FilterDefinition<MatchBase>? filter = null) //TODO Rename or remove
        {
            try
            {
                var builder = Builders<MatchBase>.Filter;
                filter ??= builder.Empty;
                filter = builder.Eq(c => c.MatchBaseType, matchBaseType)
                         & builder.Eq(c => c.MatchBaseMethod, method);
                var result = await base.GetFindFluent(filter).ToListAsync();

                return result is { Count: > 0 } ? result : Error.NoContent("MatchBase.NoContentByMatchBaseType", $"No MatchBase records exist for type {matchBaseType.ToString()} method {method.ToString()}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving MatchBases {@matchBaseType} {@method}", matchBaseType.ToString(), method.ToString());
                return Error.Failure($"MatchBase.GetByTypeAndMethodFailure", $"Error getting Documents with type {matchBaseType.ToString()} method {method.ToString()}. Error: {ex.Message}");
            }
        }

        public async Task<Result<MatchBase>> UpdateScore(string matchHash, decimal newScore)
        { 
            var updateMatchResult = await base.GetById(matchHash);
            if (!updateMatchResult.IsSuccess)
                return updateMatchResult.Error!;

            if (updateMatchResult.Value.Score == newScore)
                return Error.Validation("MatchBase.UpdateScoreValidation", "No change in score, nothing to update");

            return await this.CombinationUpdateMatchBaseScore(updateMatchResult.Value, newScore).FindAndUpdateDocument();
        }

        private CombinationPipeline<MatchBase> CombinationUpdateMatchBaseScore(MatchBase matchBase, decimal newScore)
        {
            var filter = Builders<MatchBase>.Filter.Eq(c => c.DocumentId, matchBase.DocumentId);

            return base.Update(filter).AppendUpdate(c => c.UpdateMatchBaseScore(newScore)) //TODO Should I move this into the query or leave it here as always necessary?
                                      .AppendPipeline(c => c.AppendStatus(VersionProvider.NewStatus(Status.Updated, $"Score: {newScore.ToString()}")));
        }
    }
}
