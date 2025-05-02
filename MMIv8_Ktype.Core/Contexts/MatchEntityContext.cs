using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace MMIv8_Ktype.Core.Contexts
{

    public class MatchEntityContext(MongoDBContext MMIv8_Ktype) : MongoBaseContext(MMIv8_Ktype)
    {
        public async Task<IAsyncCursor<ObjectId>> GetAllMakeModelMatchID(IMongoCollection<MatchEntity> collection, FilterDefinition<MatchEntity>? filter = null)// TODO Check speed //TODO Turn into Projections
        {
            var filterBuilder = Builders<MatchEntity>.Filter;
            filter ??= filterBuilder.Empty;

            var options = new DistinctOptions
            {
                Comment = "Get all make model match IDs"
            };

            return await collection.DistinctAsync<ObjectId>("MatchMakeModelMatchID", filter, options);
        }

        public async Task<IAsyncCursor<ObjectId>> GetAllMMIv8EntityIds(IMongoCollection<MatchEntity> collection, FilterDefinition<MatchEntity>? filter = null)// TODO Check speed //TODO Turn into Projections
        {
            var filterBuilder = Builders<MatchEntity>.Filter;
            filter ??= filterBuilder.Empty;

            var options = new DistinctOptions
            {
                Comment = "Get all MMIv8Entites",
            };

            return await collection.DistinctAsync<ObjectId>("MMIv8Entity._id", filter, options);
        }

        public IEnumerable<MatchBase> GetAllMatchBase(IMongoCollection<MatchEntity> collection, FilterDefinition<MatchEntity>? filter = null)
        {
            var filterBuilder = Builders<MatchEntity>.Filter;
            filter ??= filterBuilder.Empty;

            var collectionQuery = collection.AsQueryable();

            var matches = collectionQuery.Where(x => filter.Inject()).Select(s => new { dictionary = s.EntityComparison });

            return matches.ToList().SelectMany(x => x.dictionary.Values).GroupBy(g => g.MatchHash).Select(g => g.First());
        }

        public async Task<IAsyncCursor<MatchBase>> GetMatchBaseByType(IMongoCollection<MatchEntity> collection, MatchBaseType matchBaseType, MatchBaseMethod[]? method, string? matchHashID, bool emptyScore, FilterDefinition<MatchEntity>? filter = null)// TODO Try and convert to driver based query
        {
            var filterBuilder = Builders<MatchEntity>.Filter;
            filter ??= filterBuilder.Empty;

            var matchBaseFilter = filterBuilder.Empty;// TODO Try and convert to driver based query maybe once this is its own class
            if (method is not null)
                matchBaseFilter &= filterBuilder.In($"EntityComparison.{matchBaseType}.MatchBaseMethod", method.Select(c => c.ToString()));
            if (matchHashID is not null)
                matchBaseFilter &= filterBuilder.Eq($"EntityComparison.{matchBaseType}._id", matchHashID);
            if (emptyScore)
                matchBaseFilter &= filterBuilder.Exists($"EntityComparison.{matchBaseType}.Score", true);

            var uniqueObjects = collection.Aggregate()// TODO Try and convert to driver based query maybe once this is its own class
                .Match(filter)
                .Sort(new BsonDocument
                    {
                        { $"EntityComparison.{matchBaseType}.MatchBaseMethod", 1 },
                        { $"EntityComparison.{matchBaseType}._id", 1 },
                        { $"EntityComparison.{matchBaseType}.Score", 1 }
                    }
                )
                .Match(matchBaseFilter)
                .Group(new BsonDocument
                    {
                        { "_id", new BsonDocument
                                {
                                    { "_id", $"$EntityComparison.{matchBaseType}._id" },
                                    { "_t", $"$EntityComparison.{matchBaseType}._t" },
                                    { "MatchBaseType", $"$EntityComparison.{matchBaseType}.MatchBaseType" },
                                    { "MatchBaseMethod", $"$EntityComparison.{matchBaseType}.MatchBaseMethod" },
                                    { "TecDocEntity", $"$EntityComparison.{matchBaseType}.TecDocEntity" },
                                    { "MMIEntity", $"$EntityComparison.{matchBaseType}.MMIEntity" },
                                    { "DefaultScore", $"$EntityComparison.{matchBaseType}.DefaultScore" },
                                }
                        },
                        { "Score", new BsonDocument("$max", $"$EntityComparison.{matchBaseType}.Score") },
                        { "Status", new BsonDocument("$first", $"$EntityComparison.{matchBaseType}.Status") },
                    }
                )
                .ReplaceRoot<MatchBase>(new BsonDocument("$mergeObjects", new BsonArray { "$_id", new BsonDocument("Score", "$Score"), new BsonDocument("Status", "$Status") }));

            return await uniqueObjects.ToCursorAsync();
        }

        public async Task<long> CountUsedBaseMatches(IMongoCollection<MatchEntity> collection, MatchBase matchBase, bool? scoreMatch = null, FilterDefinition<MatchEntity>? filter = null)
        {
            var filterBuilder = Builders<MatchEntity>.Filter;
            filter ??= filterBuilder.Empty;
            filter &= filterBuilder.Eq($"EntityComparison.{matchBase.MatchBaseType}.MatchBaseMethod", matchBase.MatchBaseMethod.ToString())
                    & filterBuilder.Eq($"EntityComparison.{matchBase.MatchBaseType}._id", matchBase.MatchHash);

            if (scoreMatch != null && (bool)scoreMatch)
                filter &= filterBuilder.Eq($"EntityComparison.{matchBase.MatchBaseType}.Score", matchBase.Score);
            if (scoreMatch != null && !(bool)scoreMatch)
                filter &= filterBuilder.Ne($"EntityComparison.{matchBase.MatchBaseType}.Score", matchBase.Score);

            return await collection.CountDocumentsAsync(filter);
        }
    }
}
