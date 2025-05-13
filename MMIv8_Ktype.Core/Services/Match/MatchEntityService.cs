using Microsoft.Extensions.Options;
using MMIv8_Ktype.Api;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Indexes;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Serilog;
using System.Diagnostics;

namespace MMIv8_Ktype.Core.Services.Match
{

    public class MatchEntityService(MongoDBContext MMIv8_Ktype, IVersionProvider versionProvider) : BaseServiceWithVersion<MatchEntity, ObjectId>(MMIv8_Ktype.Collections.MatchEntity, versionProvider)
    {
        public async Task<MatchEntity?> GetByExternalIds(int KtypNr, int MMI_V8_Key)
        {
            var builder = Builders<MatchEntity>.Filter;
            var filter = builder.Eq(c => c.TecDocEntity.ExternalId, KtypNr) & builder.Eq(c => c.MMIv8Entity.ExternalId, MMI_V8_Key);

            return await base.GetFindFluent(filter: filter).FirstOrDefaultAsync();
        }

        public async Task<long> CountUsedBaseMatches(MatchBase matchBase, bool? scoreMatch = null, FilterDefinition<MatchEntity>? filter = null)
        {
            var filterBuilder = Builders<MatchEntity>.Filter;
            filter ??= filterBuilder.Empty;
            filter &= filterBuilder.Eq($"EntityComparison.{matchBase.MatchBaseType}.MatchBaseMethod", matchBase.MatchBaseMethod.ToString())
                    & filterBuilder.Eq($"EntityComparison.{matchBase.MatchBaseType}._id", matchBase.DocumentId);

            if (scoreMatch != null && (bool)scoreMatch)
                filter &= filterBuilder.Eq($"EntityComparison.{matchBase.MatchBaseType}.Score", matchBase.Score);
            if (scoreMatch != null && !(bool)scoreMatch)
                filter &= filterBuilder.Ne($"EntityComparison.{matchBase.MatchBaseType}.Score", matchBase.Score);

            return await base.CountByFilter(filter);
        }

        public async Task<IAsyncCursor<MatchBase>> GetMatchBaseByType(MatchBaseType matchBaseType, MatchBaseMethod[]? method, string? matchHashID, bool emptyScore = false, FilterDefinition<MatchEntity>? filter = null)// TODO Try and convert to driver based query
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

            var uniqueObjects = Collection.Aggregate()// TODO Try and convert to driver based query maybe once this is its own class
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

        public CombinationPipeline<MatchEntity> UpdateMissingMatchBase(MatchBase matchBase, FilterDefinition<MatchEntity>? filter = null)// TODO Try and convert to driver based query
        {
            var filterBuilder = Builders<MatchEntity>.Filter;
            filter ??= filterBuilder.Empty;
            filter &= filterBuilder.Eq($"EntityComparison.{matchBase.MatchBaseType}.MatchBaseMethod", matchBase.MatchBaseMethod.ToString())
                    & filterBuilder.Eq($"EntityComparison.{matchBase.MatchBaseType}._id", matchBase.DocumentId)
                    & filterBuilder.Ne($"EntityComparison.{matchBase.MatchBaseType}.Score", matchBase.Score);

            return base.Update(filter).AppendPipeline(c => c.UpdateMatchBase(matchBase));
        }

        public CombinationPipeline<MatchEntity> UpdateMatchBaseScoreMatchResult(MatchBase matchBase, FilterDefinition<MatchEntity>? filter = null)// TODO Try and convert to driver based query
        {
            var filterBuilder = Builders<MatchEntity>.Filter;
            filter ??= filterBuilder.Empty;
            filter &= filterBuilder.Eq($"EntityComparison.{matchBase.MatchBaseType}.MatchBaseMethod", matchBase.MatchBaseMethod.ToString())
                    & filterBuilder.Eq($"EntityComparison.{matchBase.MatchBaseType}._id", matchBase.DocumentId);

            return base.Update(filter).AppendPipeline(c => c.UpdateMatchBase(matchBase).UpdateScoreMatchResult());
        }

        [Obsolete("not in use?")]
        public CombinationPipeline<MatchEntity> UpdateEntity(SourceTecDocPC sourceEntity) //TODO Move into Source Entity Updates 
        {
            var filter = Builders<MatchEntity>.Filter.Eq(c => c.TecDocEntity.DocumentId, sourceEntity.DocumentId);

            return base.Update(filter).AppendUpdate(c => c.UpdateEntity(sourceEntity));
        }

        [Obsolete("not in use?")]
        public CombinationPipeline<MatchEntity> UpdateEntity(SourceMMIv8 sourceEntity) //TODO Move into Source Entity Updates 
        {
            var filter = Builders<MatchEntity>.Filter.Eq(c => c.MMIv8Entity.DocumentId, sourceEntity.DocumentId);

            return base.Update(filter).AppendUpdate(c => c.UpdateEntity(sourceEntity));
        }

        [Obsolete("not in use?")]
        public async Task RevalidateFailures(FilterDefinition<MatchEntity>? filter = null)
        {
            Log.Information("Started RevalidateFailures");
            var sw = Stopwatch.StartNew();

            //var deleteResult = await DeleteInvalidDates(); //TODO Confirm removal
            //Log.Information("Deleted {Count} Match Entities with Invalid Dates {Time}", deleteResult?.DeletedCount ?? 0, sw);

            var filterBuilder = Builders<MatchEntity>.Filter;
            filter ??= filterBuilder.Empty;

            sw.Restart();
            var matchEntity = await base.GetFindFluent(filter, base.SortByDocumentId()).FirstOrDefaultAsync();

            if (matchEntity is not null)
            {
                Log.Information("Starting Revalidation of Match Entities {Time}", sw);

                var matchResultUpdate = new CombinationPipeline<MatchEntity>(Collection, filter).AppendPipeline(c => c.UpdateScoreMatchResult());
                var matchResultResult = await matchResultUpdate.UpdateDocuments();

                Log.Information("Revalidated {Count} Match Entities {Time}", matchResultResult?.ModifiedCount ?? 0, sw);

                Log.Information("Starting Match Refine Update of Match Entities {Time}", sw);

                var matchRefineUpdate = await BulkCombinationUpdateMatchRefine(filter);
                var matchRefineResult = await matchRefineUpdate.CommitBulkWrite();

                Log.Information("Updated Match Refine for {Count} Match Entities {Time}", matchResultResult?.ModifiedCount ?? 0, sw);
            }

            sw.Stop();
        }

        public CombinationPipeline<MatchEntity> CombinationUpdatePreviousMatchedFlag(FilterDefinition<MatchEntity> filter, bool matchFlag)
        {
            return base.Update(filter).AppendUpdate(c => c.SetPreviousMatchedFlag(matchFlag))
                                      .AppendPipeline(c => c.AppendStatus(VersionProvider.NewStatus(Status.Updated, $"Updated Previous Match Flag")));
        }

        public CombinationPipeline<MatchEntity> CombinationUpdatePreviousMatchedFlag(int KTypNr, int MMI_V8_Key, bool matchFlag)
        {
            var matchEntityFilter = Builders<MatchEntity>.Filter.Eq(c => c.TecDocEntity.ExternalId, KTypNr)
                                  & Builders<MatchEntity>.Filter.Eq(c => c.MMIv8Entity.ExternalId, MMI_V8_Key);

            return CombinationUpdatePreviousMatchedFlag(matchEntityFilter, matchFlag);
        }

        public BulkCombinationUpdate BulkCombinationUpdatePreviousMatchedFlag(List<EntityRelation> entityRelations, bool matchFlag = true)
        {
            var bulkCombinationUpdate = new BulkCombinationUpdate(MMIv8_Ktype);

            foreach (var entityRelation in entityRelations)
            {
                bulkCombinationUpdate.AddCombinationUpdate(CombinationUpdatePreviousMatchedFlag(entityRelation.KTypNr, entityRelation.MMI_V8_Key, matchFlag));
            }

            return bulkCombinationUpdate;
        }

        public async Task<BulkCombinationUpdate> BulkCombinationUpdateMatchRefine(IEnumerable<int> mmi_V8_Keys) //TODO Move into unique MatchRefine Class
        {
            var tasks = mmi_V8_Keys.Select(c => CombinationUpdateMatchRefine(c)).ToArray();

            return new BulkCombinationUpdate(MMIv8_Ktype).AddCombinationUpdate( await Task.WhenAll(tasks) );

        }

        public async Task<BulkCombinationUpdate> BulkCombinationUpdateMatchRefine(FilterDefinition<MatchEntity> filter) //TODO Move into unique MatchRefine Class
        {
            return new BulkCombinationUpdate(MMIv8_Ktype).AddCombinationUpdate(await CombinationUpdateMatchRefinesList(filter));
        }

        public async Task<IEnumerable<CombinationPipeline<MatchEntity>>> CombinationUpdateMatchRefinesList(FilterDefinition<MatchEntity> filter) //TODO Move into unique MatchRefine Class
        {
            List<CombinationPipeline<MatchEntity>> combinationUpdates = new();
            await foreach (var combinationUpdate in CombinationUpdateMatchRefines(filter))
            {
                combinationUpdates.Add(combinationUpdate);
            }
            return combinationUpdates;
        }

        public async IAsyncEnumerable<CombinationPipeline<MatchEntity>> CombinationUpdateMatchRefines(FilterDefinition<MatchEntity> filter) //TODO Move into unique MatchRefine Class
        {
            using var mmiv8Entities = await base.GetDistinctCursor<int>("MMIv8Entity.ExternalId", filter);
            {
                while (await mmiv8Entities.MoveNextAsync())
                {
                    foreach (var mmiv8EntityId in mmiv8Entities.Current)
                    {
                        yield return await CombinationUpdateMatchRefine(mmiv8EntityId);
                    }
                }
            }
        }

        public async Task<CombinationPipeline<MatchEntity>> CombinationUpdateMatchRefine(int mmi_V8_Key) //TODO Move into unique MatchRefine Class
        {
            var entityFilter = Builders<MatchEntity>.Filter.Eq(c => c.MMIv8Entity.ExternalId, mmi_V8_Key);
            var matchEntities = await base.GetFindFluent(entityFilter).ToListAsync();

            return base.Update(entityFilter).AppendUpdate(c => c.UpdateMatchRefine(new(matchEntities.ToArray())));
        }

        public async Task<DeleteResult?> DeleteInvalidDates() 
        {
            var filterBuilder = Builders<MatchEntity>.Filter;
            var filter = filterBuilder.Eq(c => c.DateIntersection.date_Intersection, 0);

            var count = await base.CountByFilter(filter);

            if (count == 0)
                return null;

            Log.Information("Deleting Entity Matches with 0 Date intersection");

            return await DeleteByFilter(filter);      
        }
    }
}
