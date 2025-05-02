using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Indexes;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;
using System.Diagnostics;

namespace MMIv8_Ktype.Core.Services.Match
{
    public class MatchEntityService(MongoDBContext MMIv8_Ktype,
                                    MatchEntityContext MatchEntityContext,
                                    IVersionProvider versionProvider) : IMongoCollectionService<MatchEntity> //TODO Move MatchEntityContext into here
    {
        public IMongoCollection<MatchEntity> Collection => MMIv8_Ktype.Collections.MatchEntity;

        public async Task<MatchEntity?> GetMatchEntity(int KtypNr, int MMI_V8_Key)
        {
            var builder = Builders<MatchEntity>.Filter;
            var filter = builder.Eq(c => c.TecDocEntity.ExternalId, KtypNr) & builder.Eq(c => c.MMIv8Entity.ExternalId, MMI_V8_Key);

            return await MatchEntityContext.GetSingleDocument(Collection, filter: filter);
        }

        public async Task<MatchEntity?> GetSingleMatchEntity(FilterDefinition<MatchEntity>? filter = null)
        {
            return await MatchEntityContext.GetSingleDocument(Collection, filter: filter);
        }

        public async Task<IAsyncCursor<MatchEntity>> GetAll(FilterDefinition<MatchEntity>? filter = null, int? batchSize = 10000)
        {
            return await MatchEntityContext.GetCursor(Collection, filter: filter, batchSize: batchSize);
        }

        public IQueryable<MatchEntity> GetQuery()
        {
            return MatchEntityContext.GetQuery(Collection);
        }

        public async Task<PagedResponse<MatchEntity>> PageAll(PagedSortFilter<MatchEntity> request)
        {
            SortDefinition<MatchEntity> newSort;
            if (request.Sort is null)
                newSort = Builders<MatchEntity>.Sort.Ascending(c => c.MatchEntityID);
            else
                newSort = request.Sort.Ascending(c => c.MatchEntityID);

            return await MatchEntityContext.PaginateDocuments(Collection, newSort, request.Filter, request.PagedRequest.Page, request.PagedRequest.PageSize ?? 100);
        }

        public async Task<IAsyncCursor<ObjectId>> GetAllMakeModelMatchID(FilterDefinition<MatchEntity>? filter = null)
        {
            var builder = Builders<MatchEntity>.Filter;
            filter ??= builder.Empty;

            return await MatchEntityContext.GetAllMakeModelMatchID(Collection, filter);
        }

        public async Task<long> CountUsedBaseMatches(MatchBase matchBase, FilterDefinition<MatchEntity>? filter = null)
        {
            return await MatchEntityContext.CountUsedBaseMatches(Collection, matchBase, filter: filter);
        }

        public async Task CreateEntityMatch(List<MatchEntity> newMatches)
        {
            await MatchEntityContext.Create(Collection, newMatches.ToArray());
        }

        [Obsolete("Not in use?",true)]
        public async Task CreateEntityMatch(MatchEntity newMatch) // TODO Simplify these calls so they only validate and do the thing e.g Add a new record. Then make new calls in mapping to actually build the record
        {
            var builder = Builders<MatchEntity>.Filter;
            var filter = builder.Eq(m => m.TecDocEntity.SourceEntityID, newMatch.TecDocEntity.SourceEntityID) & builder.Eq(m => m.MMIv8Entity.SourceEntityID, newMatch.MMIv8Entity.SourceEntityID);

            if (await MatchEntityContext.GetSingleDocument(Collection, filter) is not null)
                throw new Exception($"Entity match TD: {newMatch.TecDocEntity.SourceEntityID} and MMI: {newMatch.MMIv8Entity.SourceEntityID} already exists");

            await MatchEntityContext.Create(Collection, newMatch);
        }

        public async Task<IAsyncCursor<MatchBase>> GetMatchBase(MatchBaseType matchBaseType, MatchBaseMethod[]? method = null, string? matchHashID = null, bool emptyScore = false, FilterDefinition<MatchEntity>? filter = null)
        {
            return await MatchEntityContext.GetMatchBaseByType(Collection, matchBaseType, method, matchHashID, emptyScore, filter);
        }

        public CombinationPipeline<MatchEntity> UpdateMatchEntity(MatchEntity matchEntity)
        {
            var filter = Builders<MatchEntity>.Filter.Eq(c => c.MatchEntityID, matchEntity.MatchEntityID);

            return new CombinationPipeline<MatchEntity>(Collection, filter);
        }

        public CombinationPipeline<MatchEntity> UpdateMissingMatchBase(MatchBase matchBase, FilterDefinition<MatchEntity>? filter = null)// TODO Try and convert to driver based query
        {
            var filterBuilder = Builders<MatchEntity>.Filter;
            filter ??= filterBuilder.Empty;
            filter &= filterBuilder.Eq($"EntityComparison.{matchBase.MatchBaseType}.MatchBaseMethod", matchBase.MatchBaseMethod.ToString())
                    & filterBuilder.Eq($"EntityComparison.{matchBase.MatchBaseType}._id", matchBase.MatchHash)
                    & filterBuilder.Ne($"EntityComparison.{matchBase.MatchBaseType}.Score", matchBase.Score);

            return new CombinationPipeline<MatchEntity>(Collection, filter).AppendPipeline(c => c.UpdateMatchBase(matchBase));
        }

        public CombinationPipeline<MatchEntity> UpdateMatchBaseScoreMatchResult(MatchBase matchBase, FilterDefinition<MatchEntity>? filter = null)// TODO Try and convert to driver based query
        {
            var filterBuilder = Builders<MatchEntity>.Filter;
            filter ??= filterBuilder.Empty;
            filter &= filterBuilder.Eq($"EntityComparison.{matchBase.MatchBaseType}.MatchBaseMethod", matchBase.MatchBaseMethod.ToString())
                    & filterBuilder.Eq($"EntityComparison.{matchBase.MatchBaseType}._id", matchBase.MatchHash);

            return new CombinationPipeline<MatchEntity>(Collection, filter).AppendPipeline(c => c.UpdateMatchBase(matchBase)
                                                                                                 .UpdateScoreMatchResult());
        }

        //public CombinationPipeline<MatchEntity> UpdateEntity(SourceEntity sourceEntity) //TODO check this is still updating properly
        //{
        //    var filter = Builders<MatchEntity>.Filter.Eq(c => c.TecDocEntity.SourceEntityID, sourceEntity.SourceEntityID);

        //    return new CombinationPipeline<MatchEntity>(Collection, filter).AppendUpdate(c => c.UpdateEntity(sourceEntity));
        //}

        public CombinationPipeline<MatchEntity> UpdateEntity(MongoSourceTecDocPC sourceEntity)
        {
            var filter = Builders<MatchEntity>.Filter.Eq(c => c.TecDocEntity.SourceEntityID, sourceEntity.SourceEntityID);

            return new CombinationPipeline<MatchEntity>(Collection, filter).AppendUpdate(c => c.UpdateEntity(sourceEntity));
        }

        public CombinationPipeline<MatchEntity> UpdateEntity(MongoSourceMMIv8 sourceEntity)
        {
            var filter = Builders<MatchEntity>.Filter.Eq(c => c.MMIv8Entity.SourceEntityID, sourceEntity.SourceEntityID);

            return new CombinationPipeline<MatchEntity>(Collection, filter).AppendUpdate(c => c.UpdateEntity(sourceEntity));
        }

        public async Task RevalidateFailures(FilterDefinition<MatchEntity>? filter = null)
        {
            Log.Information("Started RevalidateFailures");
            var sw = Stopwatch.StartNew();

            var deleteResult = await DeleteInvalidDates();
            Log.Information("Deleted {Count} Match Entities with Invalid Dates {Time}", deleteResult?.DeletedCount ?? 0, sw);

            var filterBuilder = Builders<MatchEntity>.Filter;
            filter ??= filterBuilder.Empty;

            sw.Restart();
            var matchEntity = await MatchEntityContext.GetSingleDocument(Collection, filter, Builders<MatchEntity>.Sort.Ascending(c => c.MatchEntityID));

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
            return new CombinationPipeline<MatchEntity>(Collection, filter).AppendUpdate(c => c.SetPreviousMatchedFlag(matchFlag))
                                                                           .AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Updated, $"Updated Previous Match Flag")));
        }

        public CombinationPipeline<MatchEntity> CombinationUpdatePreviousMatchedFlag(int KTypNr, int MMI_V8_Key, bool matchFlag)
        {
            //    var matchEntityFilter = Builders<MatchEntity>.Filter.Eq(c => c.TecDocEntity.ExternalId, KTypNr)
            //                          & Builders<MatchEntity>.Filter.Eq(c => c.MMIv8Entity.ExternalId, MMI_V8_Key);
            var matchEntityFilter = Builders<MatchEntity>.Filter.Eq(c => c.TecDocEntity.KTypNr, KTypNr) //REPLACE
                                  & Builders<MatchEntity>.Filter.Eq(c => c.MMIv8Entity.MMI_V8_Key, MMI_V8_Key);

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
            var filter = Builders<MatchEntity>.Filter.In(c => c.MMIv8Entity.ExternalId, mmi_V8_Keys);
            return await BulkCombinationUpdateMatchRefine(filter);
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
            using var mmiv8Entities = await MatchEntityContext.GetAllMMIv8EntityIds(Collection, filter);
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

        public async Task<CombinationPipeline<MatchEntity>> CombinationUpdateMatchRefine(ObjectId mmiv8EntityId) //TODO Move into unique MatchRefine Class
        {
            var entityFilter = Builders<MatchEntity>.Filter.Eq(c => c.MMIv8Entity.SourceEntityID, mmiv8EntityId);
            var matchEntities = await MatchEntityContext.GetMultipleDocuments(Collection, entityFilter);

            return new CombinationPipeline<MatchEntity>(Collection, entityFilter).AppendUpdate(c => c.UpdateMatchRefine( new(matchEntities.ToArray()) ));
        }

        public async Task<DeleteResult?> DeleteInvalidDates() 
        {
            var filterBuilder = Builders<MatchEntity>.Filter;
            var filter = filterBuilder.Eq(c => c.DateIntersection.date_Intersection, 0);

            var count = await MatchEntityContext.CountByFilter(Collection, filter);

            if (count == 0)
                return null;

            Log.Information("Deleting Entity Matches with 0 Date intersection");

            return await Delete(filter);      
        }

        public async Task<DeleteResult> Delete(FilterDefinition<MatchEntity> filter)
        {
            return await MatchEntityContext.Delete(Collection, filter);
        }

        public async Task<DeleteResult> DeleteAll()
        {
            return await MatchEntityContext.Delete(Collection, Builders<MatchEntity>.Filter.Empty);
        }
    }
}
