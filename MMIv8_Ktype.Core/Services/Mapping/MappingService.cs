using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Core.Endpoints;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Core.Services.Source;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;
using System.Diagnostics;

namespace MMIv8_Ktype.Core.Services.Mapping
{
    public class MappingService(MongoDBContext MMIv8_Ktype,
                                MatchMakeModelService MatchMakeModelService,
                                SourceMMIv8EntityModelService SourceMMIv8EntityModelService,
                                SourceTecDocEntityModelService SourceTecDocEntityModelService,
                                MatchEntityService MatchEntityService,
                                MatchBaseService MatchBaseService,
                                SourceMMIv8Service SourceMMIv8Service,
                                SourceTecDocPCService SourceTecDocPCService,
                                IVersionProvider versionProvider)
    {
        #region Match Make Model

        public async Task CreateModelMatch(List<MatchMakeModelRequest> matchMakeModels)
        {
            List<ObjectId> createdMakeModels = new();
            List<Task> tasks = new();

            foreach (var matchMakeModel in matchMakeModels)
            {
                var existingModelMatch = await MatchMakeModelService.GetByModelIds(matchMakeModel);

                if (existingModelMatch is not null)
                {
                    Log.Information("MakeModelMatch already exists, skipping match: TD '{TD_SourceEntityModelHash}' MMI '{MMI_SourceEntityModelHash}'", matchMakeModel.TD_SourceEntityModelHash, matchMakeModel.MMI_SourceEntityModelHash);
                    continue;
                }

                MatchMakeModel newMatchMakeModel = new(
                    tecDocModel: matchMakeModel.TD_SourceEntityModelHash == string.Empty ? new() : await SourceTecDocEntityModelService.GetById(matchMakeModel.TD_SourceEntityModelHash) ?? new(),
                    mmiv8Model: matchMakeModel.MMI_SourceEntityModelHash == string.Empty ? new() : await SourceMMIv8EntityModelService.GetById(matchMakeModel.MMI_SourceEntityModelHash) ?? new(),
                    versionProvider
                    );

                createdMakeModels.Add(newMatchMakeModel.MatchID);
                await MatchMakeModelService.Create(newMatchMakeModel);

                if (newMatchMakeModel.TecDocModel.SourceEntityModelHash is not null && newMatchMakeModel.MMIv8Model.SourceEntityModelHash is not null)
                {
                    await StoreEntityMatch(newMatchMakeModel);

                    var filter = Builders<MatchEntity>.Filter.Eq(c => c.MatchMakeModelMatchID, newMatchMakeModel.MatchID);
                    tasks.Add(RecalculateMatchBase(filter));
                }
            }

            await Task.WhenAll(tasks);
        }

        public async Task<ObjectId> CreateMakeModelMatch(MatchMakeModelRequest matchMakeModel)
        {
            var existingModelMatch = await MatchMakeModelService.GetByModelIds(matchMakeModel);

            if (existingModelMatch is not null)
            {
                Log.Information("MakeModelMatch already exists, skipping match: TD '{TD_SourceEntityModelHash}' MMI '{MMI_SourceEntityModelHash}'", matchMakeModel.TD_SourceEntityModelHash, matchMakeModel.MMI_SourceEntityModelHash);
                return existingModelMatch.MatchID;
            }

            MatchMakeModel newMatchMakeModel = new(
                tecDocModel: matchMakeModel.TD_SourceEntityModelHash == string.Empty ? new() : await SourceTecDocEntityModelService.GetById(matchMakeModel.TD_SourceEntityModelHash) ?? new(),
                mmiv8Model: matchMakeModel.MMI_SourceEntityModelHash == string.Empty ? new() : await SourceMMIv8EntityModelService.GetById(matchMakeModel.MMI_SourceEntityModelHash) ?? new(),
                versionProvider
                );

            await MatchMakeModelService.Create(newMatchMakeModel);

            if (newMatchMakeModel.TecDocModel.SourceEntityModelHash is not null && newMatchMakeModel.MMIv8Model.SourceEntityModelHash is not null)
            {
                await StoreEntityMatch(newMatchMakeModel);

                var filter = Builders<MatchEntity>.Filter.Eq(c => c.MatchMakeModelMatchID, newMatchMakeModel.MatchID);
                await RecalculateMatchBase(filter);
            }

            return newMatchMakeModel.MatchID;
        }

        public async Task DeleteModelMatch(List<MatchMakeModelRequest> matchMakeModels)
        {
            foreach (var matchMakeModel in matchMakeModels)
            {
                var modelMatch = await MatchMakeModelService.GetByModelIds(matchMakeModel);

                if (modelMatch is null)
                {
                    Log.Information("MakeModelMatch does not exist, skipping deletion: TD '{TD_SourceEntityModelHash}' MMI '{MMI_SourceEntityModelHash}'", matchMakeModel.TD_SourceEntityModelHash, matchMakeModel.MMI_SourceEntityModelHash);
                    continue;
                }

                await MatchMakeModelService.Delete(modelMatch);

                var filterBuilder = Builders<MatchEntity>.Filter;
                var filter = filterBuilder.Eq(c => c.MatchMakeModelMatchID, modelMatch.MatchID);

                await MatchEntityService.Delete(filter);
            }
        }

        public async Task<ObjectId?> DeleteMakeModelMatch(MatchMakeModelRequest matchMakeModel)
        {
            var modelMatch = await MatchMakeModelService.GetByModelIds(matchMakeModel);

            if (modelMatch is null)
            {
                Log.Information("MakeModelMatch does not exist, skipping deletion: TD '{TD_SourceEntityModelHash}' MMI '{MMI_SourceEntityModelHash}'", matchMakeModel.TD_SourceEntityModelHash, matchMakeModel.MMI_SourceEntityModelHash);
                return null;
            }

            await MatchMakeModelService.Delete(modelMatch);

            var filterBuilder = Builders<MatchEntity>.Filter;
            var filter = filterBuilder.Eq(c => c.MatchMakeModelMatchID, modelMatch.MatchID);

            await MatchEntityService.Delete(filter);

            return modelMatch.MatchID;
        }

        public async Task<ObjectId?> DeleteMakeModelMatch(ObjectId matchID)
        {
            var modelMatch = await MatchMakeModelService.GetById(matchID);

            if (modelMatch is null)
            {
                Log.Information("MakeModelMatch does not exist {matchID}", matchID);
                return null;
            }

            await MatchMakeModelService.Delete(modelMatch);

            var filterBuilder = Builders<MatchEntity>.Filter;
            var filter = filterBuilder.Eq(c => c.MatchMakeModelMatchID, modelMatch.MatchID);

            await MatchEntityService.Delete(filter);

            return modelMatch.MatchID;
        }

        #endregion

        #region Match Base


        public async Task UpdateMatchScore(MatchBaseType matchBaseType, string matchHash, decimal newScore)
        {
            try
            {
                var updateMatch = await MatchBaseService.GetById(matchHash) ?? throw new Exception("Match does not exist");

                if (updateMatch.Score != newScore)
                {
                    CombinationPipeline<MatchBase> matchBaseUpdate = MatchBaseService.UpdateScore(updateMatch, newScore);
                    updateMatch = await matchBaseUpdate.FindAndUpdateDocument();

                    CombinationPipeline<MatchEntity> matchEntityUpdate = MatchEntityService.UpdateMatchBaseScoreMatchResult(updateMatch); //TODO this might need to be recalculate
                    var matchEntityResult = await matchEntityUpdate.UpdateDocuments();
                    Log.Information("Updated {count} MatchEntities for {MatchBase}", matchEntityResult?.ModifiedCount ?? 0, updateMatch.ToString());

                    BulkCombinationUpdate matchRefineUpdate = await MatchEntityService.BulkCombinationUpdateMatchRefine(matchEntityUpdate.Filter);
                    var matchRefineResult = await matchRefineUpdate.CommitBulkWrite();
                    Log.Information("Updated Match Refine of {count} MatchEntities", matchRefineResult?.ModifiedCount ?? 0);
                }
                else if (updateMatch.Status.Current.Status == Status.Check)
                {
                    CombinationPipeline<MatchBase> matchBaseUpdate = MatchBaseService.UpdateMatchBase(updateMatch).AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Check)));
                    var matchBaseResult = await matchBaseUpdate.UpdateDocuments();
                    Log.Information("Updated {MatchBase} Status to Checked", updateMatch.ToString(), matchBaseResult?.ModifiedCount ?? 0);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Cannot update score match {MatchHash}", matchHash);
            }
        }

        public async Task CheckMatchBaseDeprecated()//TODO Utilise Deprecate Match Base
        {
            var currentMatch = await MatchBaseService.GetAll(batchSize: 1000);

            while (await currentMatch.MoveNextAsync())
            {
                foreach (var matchBase in currentMatch.Current)
                {
                    await DeprecateMatchBase(matchBase);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="matchBase"></param>
        /// <returns>returns true if the MatchBase is deprecated</returns>
        public async Task<bool> DeprecateMatchBase(MatchBase matchBase)
        {
            var matchBaseCount = await MatchEntityService.CountUsedBaseMatches(matchBase);

            bool isMissing = matchBaseCount == 0;

            bool isDeprecated = matchBase.Status.Current.Status == Status.Deprecated;

            if (isMissing == isDeprecated)
                return isDeprecated;
            else if (isMissing)
                _ = await MatchBaseService.UpdateMatchBase(matchBase).AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Deprecated))).UpdateDocuments();
            else
                _ = await MatchBaseService.UpdateMatchBase(matchBase).AppendPipeline(c => c.RemoveStatus(Status.Deprecated)).UpdateDocuments();

            return isMissing;
        }

        public async Task RecalculateMatchBase(FilterDefinition<MatchEntity> filter, MatchBaseType matchBaseType)
        {
            Log.Debug($"Gathering MatchBases for Recalculation");

            var matchEntityManual = await MatchEntityService.GetMatchBase(matchBaseType, [ MatchBaseMethod.Manual, MatchBaseMethod.Partial ], null, false, filter);
            Dictionary<MatchBaseType, IEnumerable<MatchBase>> matchBaseDict = new() { { matchBaseType, await matchEntityManual.ToListAsync() } };

            Log.Debug("Retrived {Count} for {MatchBaseType}", matchBaseDict[ matchBaseType ].Count(), matchBaseType);

            await CalculateMatchBase(matchBaseDict, filter);
        }

        public async Task RecalculateMatchBase(FilterDefinition<MatchEntity> filter)
        {
            Log.Debug($"Gathering MatchBases for Recalculation");

            Dictionary<MatchBaseType, IEnumerable<MatchBase>> matchBaseDict = new();
            foreach (MatchBaseType matchBaseType in (MatchBaseType[])Enum.GetValues(typeof(MatchBaseType)))
            {
                var matchEntityManual = await MatchEntityService.GetMatchBase(matchBaseType, [ MatchBaseMethod.Manual, MatchBaseMethod.Partial ], null, false, filter);
                matchBaseDict.Add(matchBaseType, matchEntityManual.ToList());

                Log.Debug("Retrived {Count} for {MatchBaseType}", matchBaseDict[ matchBaseType ].Count(), matchBaseType);
            }
            await CalculateMatchBase(matchBaseDict, filter);
        }

        public async Task RecalculateMatchBase()
        {
            Log.Debug($"Gathering MatchBases for Recalculation");

            Dictionary<MatchBaseType, IEnumerable<MatchBase>> matchBaseDict = new();
            foreach (MatchBaseType matchBaseType in (MatchBaseType[])Enum.GetValues(typeof(MatchBaseType)))
            {
                var matchEntityManual = await MatchEntityService.GetMatchBase(matchBaseType, [ MatchBaseMethod.Manual ], null, false);
                var matchBasePartial = await MatchBaseService.GetAll(matchBaseType, MatchBaseMethod.Partial, Builders<MatchBase>.Filter.Ne(x => x.Status.Current.Status, Status.Deprecated));
                matchBaseDict.Add(matchBaseType, [ .. matchEntityManual.ToList(), .. matchBasePartial.ToList() ]);

                Log.Debug("Retrived {Count} for {MatchBaseType}", matchBaseDict[ matchBaseType ].Count(), matchBaseType);
            }
            await CalculateMatchBase(matchBaseDict);
        }

        public async Task CalculateMatchBase(Dictionary<MatchBaseType, IEnumerable<MatchBase>> matchBaseDict, FilterDefinition<MatchEntity>? filter = null)
        {
            filter ??= Builders<MatchEntity>.Filter.Empty;

            var sw = Stopwatch.StartNew();

            foreach (var groupedMatchBase in matchBaseDict.Where(c => c.Value.Any()))
            {
                Log.Debug("Starting {MatchBaseType} {time}", groupedMatchBase.Key, sw);

                var existingMatchBases = (await MatchBaseService.GetAll(groupedMatchBase.Key)).ToList();

                existingMatchBases = existingMatchBases.IntersectBy(groupedMatchBase.Value.Select(c => c.MatchHash), c => c.MatchHash).ToList();
                var newMatchBases = groupedMatchBase.Value.Where(c => c.MatchBaseMethod == MatchBaseMethod.Manual).ExceptBy(existingMatchBases.Select(c => c.MatchHash), c => c.MatchHash);

                if (existingMatchBases.Any())
                {
                    List<BulkWriteModel> bulks = new();
                    foreach (var existingMatchBase in existingMatchBases)
                    {
                        var newMatchBase = existingMatchBase;
                        if (existingMatchBase.Status.Current.Status == Status.Deprecated)
                        {
                            var matchBaseUpdate = MatchBaseService.UpdateMatchBase(existingMatchBase).AppendPipeline(c => c.RemoveStatus(Status.Deprecated));
                            var matchBaseResult = await matchBaseUpdate.UpdateDocuments();

                            newMatchBase = await MatchBaseService.GetById(existingMatchBase.MatchHash) ?? existingMatchBase;
                        }

                        CombinationPipeline<MatchEntity> matchEntityUpdate = MatchEntityService.UpdateMissingMatchBase(newMatchBase, filter);
                        var matchEntityUpdateResult = await matchEntityUpdate.UpdateDocuments();
                                                
                    }
                }

                if (newMatchBases.Any())
                {
                    await MatchBaseService.CreateBulk(newMatchBases.ToList());
                }

                Log.Information("Recalculating {MatchBaseType} {ExistsCount} existing to check, {NewCount} new to create {time}", groupedMatchBase.Key, existingMatchBases.Count(), newMatchBases.Count(), sw);
            }

            Log.Information("Completed All MatchBase Recalculations {time}", sw);

            sw.Restart();


            Log.Debug("Starting Revalidation {time}", sw);
            var validationResult = await new CombinationPipeline<MatchEntity>(MatchEntityService.Collection, filter)
                                                            .AppendPipeline(c => c.UpdateScoreMatchResult())
                                                            .UpdateDocuments();
            Log.Debug("Finished Revalidation {time}", sw);


            Log.Debug("Starting MatchRefine Update {time}", sw);

            BulkCombinationUpdate bulkMatchRefineUpdate = await MatchEntityService.BulkCombinationUpdateMatchRefine(filter);
            var bulkMatchRefineResult = await bulkMatchRefineUpdate.CommitBulkWrite();

            Log.Information("Updated Match Refine for {count} matches {time}", bulkMatchRefineResult.ModifiedCount, sw);
            

        }

        //public async Task CalculateMatchBase(Dictionary<MatchBaseType, IEnumerable<MatchBase>> matchBaseDict, FilterDefinition<MatchEntity>? filter = null)
        //{
        //    filter ??= Builders<MatchEntity>.Filter.Empty;
        //    bool revalidateFailures = false;

        //    foreach (var groupedMatchBase in matchBaseDict.Where(c => c.Value.Any()))
        //    {
        //        Log.Debug("Starting {MatchBaseType}", groupedMatchBase.Key);

        //        var existingMatchBases = (await MatchBaseService.GetAll(groupedMatchBase.Key)).ToList();

        //        existingMatchBases = existingMatchBases.IntersectBy(groupedMatchBase.Value.Select(c => c.MatchHash), c => c.MatchHash).ToList();
        //        var newMatchBases = groupedMatchBase.Value.Where(c => c.MatchBaseMethod == MatchBaseMethod.Manual).ExceptBy(existingMatchBases.Select(c => c.MatchHash), c => c.MatchHash);

        //        Log.Information("Recalculating {MatchBaseType} {ExistsCount} existing to check, {NewCount} new to create", groupedMatchBase.Key, existingMatchBases.Count(), newMatchBases.Count());

        //        if (existingMatchBases.Any())
        //        {
        //            List<BulkWriteModel> bulks = new();
        //            foreach (var existingMatchBase in existingMatchBases)
        //            {
        //                if (existingMatchBase.Status.Current.Status == Status.Deprecated)
        //                {
        //                    await MatchBaseService.RemoveStatus(existingMatchBase, Status.Deprecated);
        //                    existingMatchBase.Status.History.Pop();
        //                }

        //                var result = await MatchEntityService.UpdateMatchBase(existingMatchBase, filter);
        //                revalidateFailures = revalidateFailures || result?.ModifiedCount > 0;
        //            }

        //        }

        //        if (newMatchBases.Any())
        //        {
        //            await MatchBaseService.CreateBulk(newMatchBases.ToList());
        //        }
        //    }

        //    if (revalidateFailures)
        //    {
        //        await MatchEntityService.RevalidateFailures(filter);
        //    }
        //}

        public async Task StorePartialMatchBase(MatchBaseType matchBaseType, string matchHash, decimal? newScore = null)
        {
            var matchEntityPartials = await MatchEntityService.GetMatchBase(matchBaseType, [ MatchBaseMethod.Partial ], matchHash);

            var matchEntityPartial = matchEntityPartials.ToList().FirstOrDefault();

            if (matchEntityPartial is null)
                return;

            await MatchBaseService.Create(matchBaseType, matchEntityPartial);

            if (newScore is not null)
                await UpdateMatchScore(matchBaseType, matchHash, newScore ?? 0);
        }

        public async Task RemovePartialMatchBase(MatchBaseType matchBaseType, string matchHash)
        {
            var matchEntityPartial = await MatchBaseService.GetById(matchHash);

            if (matchEntityPartial is null)
                return;


            await UpdateMatchScore(matchBaseType, matchHash, matchEntityPartial.Reset(versionProvider).Score);

            await MatchBaseService.Delete(matchBaseType, matchHash);
        }

        #endregion

        #region Match Entity

        public async Task<MatchEntity> CheckMatchVadlidity(int KtypNr, int MMI_V8_Key)
        {
            var currentMatch = await MatchEntityService.GetMatchEntity(KtypNr, MMI_V8_Key);
            if (currentMatch is not null)
                return currentMatch;

            var tecdocEntity = await SourceTecDocPCService.GetByExternalId(KtypNr);
            var mmiEntity = await SourceMMIv8Service.GetByExternalId(MMI_V8_Key);

            if (tecdocEntity is null || mmiEntity is null)
                throw new Exception($"Couldn't find Entities, found Ktype {tecdocEntity is not null} / MMI {mmiEntity is not null}");

            MatchMakeModelRequest makeModel = new( tecdocEntity.SourceEntityModelHash, mmiEntity.SourceEntityModelHash );
            var makeModelMatch = await MatchMakeModelService.GetByModelIds(makeModel);

            ObjectId makeModelMatchID = new();

            if (makeModelMatch is null)
            {
                Log.Warning("Make Model Match doesn't exist");
            }
            else
            {
                makeModelMatchID = makeModelMatch.MatchID;
            }

            var newMatch = new MatchEntity(versionProvider, tecdocEntity, mmiEntity, makeModelMatchID);

            foreach (var matchBase in newMatch.EntityComparison.Values.Where(c => c.MatchBaseMethod == MatchBaseMethod.Manual || c.MatchBaseMethod == MatchBaseMethod.Partial))
            {
                var currentMatchBase = await MatchBaseService.GetById(matchBase.MatchHash);

                if (currentMatchBase is null)
                    continue;
                
                newMatch.EntityComparison[ matchBase.MatchBaseType ] = currentMatchBase;
            }

            newMatch.UpdateScore();

            return newMatch;
        }

        public async Task StoreEntityMatch(MatchMakeModel makeModelMatch) // memory heavy
        {
            List<Task> createTasks = new();
            using (var tecdocEntities = await SourceTecDocPCService.GetByModelId(makeModelMatch.TecDocModel.SourceEntityModelHash))
            using (var mmiEntities = await SourceMMIv8Service.GetByModelId(makeModelMatch.MMIv8Model.SourceEntityModelHash))
            {
                var newMatches = GenerateEntityMatch(await tecdocEntities.ToListAsync(), await mmiEntities.ToListAsync(), makeModelMatch.MatchID);

                await foreach (var match in newMatches)
                {
                    if (match.Any())
                        createTasks.Add(MatchEntityService.BulkCreateEntityMatch(match.ToList()));
                }
            }

            Task.WaitAll(createTasks.ToArray());
        }

        public async IAsyncEnumerable<IEnumerable<MatchEntity>> GenerateEntityMatch(IEnumerable<MongoSourceTecDocPC> tecdocEntities, IEnumerable<MongoSourceMMIv8> mmiEntities, ObjectId MakeModelMatchID)
        {
            foreach (var tecdocEntity in tecdocEntities) //TODO Create previous match collection and then keep these updated
            {
                var query = mmiEntities.AsParallel().Select(m => new MatchEntity(versionProvider, tecdocEntity, m, MakeModelMatchID)).Where(m => m.DateIntersection.date_Intersection != 0);

                yield return query.ToList();
            }
        }

        public async Task UpdatePreviousMatchedFlag(IEnumerable<UpdateFlagRequest> input) // TODO Change to single MMI_V8_Key
        {
            var filterBuilder = Builders<MatchEntity>.Filter;
            var filter = filterBuilder.Empty;

            List<Task<BulkCombinationUpdate>> tasks = new();

            var mmiUpdates = input.GroupBy(c => c.MMI_V8_Key).ToDictionary(c => c.Key, c => c.ToList());

            foreach(var batch in mmiUpdates.Chunk(1000))
            {
                BulkCombinationUpdate bulkCombinationUpdate = new(MMIv8_Ktype);

                foreach (var mmi_Entity in batch)
                {
                    foreach (var match in mmi_Entity.Value)
                    {
                        filter = filterBuilder.Eq(c => c.TecDocEntity.ExternalId, match.KTypNr)
                               & filterBuilder.Eq(c => c.MMIv8Entity.ExternalId, match.MMI_V8_Key);

                        CombinationPipeline<MatchEntity> combinationUpdate = new(MatchEntityService.Collection, filter);
                        combinationUpdate.AppendUpdate(c => c.SetPreviousMatchedFlag(match.Flag))
                                         .AppendPipeline(c => c.AppendStatus( versionProvider.NewStatus(Status.Checked, $"Updated Previous Match Flag {match.Detail}") ));

                        bulkCombinationUpdate.AddCombinationUpdate(combinationUpdate);
                    }
                }

                ClientBulkWriteResult? bulkResult = await bulkCombinationUpdate.CommitBulkWrite();
                Log.Information("Updated Previous Match Flag for {count} matches", bulkResult.ModifiedCount);

                tasks.Add(MatchEntityService.BulkCombinationUpdateMatchRefine(batch.Select(c => c.Key)));
            }

            var matchRefineUpdates = await Task.WhenAll(tasks);

            await Task.Run(() => matchRefineUpdates.ToList().ForEach(c => c.CommitBulkWrite()));

            Log.Information("Updated Previous Matched Flags");
        }

        public async Task UpdateMatchedFlag(IEnumerable<UpdateFlagRequest> input) // TODO Update to reflect UpdatePreviousMatchedFlag
        {
            var filterBuilder = Builders<MatchEntity>.Filter;
            var filter = filterBuilder.Empty;

            List<Task<IEnumerable<CombinationPipeline<MatchEntity>>>> tasks = new();

            foreach (var inputBatch in input.ToDictionary(c => c.MMI_V8_Key, c => c).Chunk(1000))
            {
                BulkCombinationUpdate bulkCombinationUpdate = new(MMIv8_Ktype);

                foreach (var matches in inputBatch.Select(c => c.Value))
                {
                    filter = filterBuilder.Eq(c => c.TecDocEntity.ExternalId, matches.KTypNr)
                                    & filterBuilder.Eq(c => c.MMIv8Entity.ExternalId, matches.MMI_V8_Key);

                    CombinationPipeline<MatchEntity> combinationPipeline = new(MatchEntityService.Collection, filter);
                    combinationPipeline.AppendUpdate(c => c.SetMatchedFlag(matches.Flag))
                                       .AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Checked, $"Updated Matched Flag {matches.Detail}")));

                    bulkCombinationUpdate.AddCombinationUpdate(combinationPipeline);
                }

                var bulkResult = await bulkCombinationUpdate.CommitBulkWrite();
                Log.Information("Updated Matched Flag for {count} matches", bulkResult.ModifiedCount);

                var updateMatchRefineFilter = filterBuilder.In(c => c.MMIv8Entity.ExternalId, inputBatch.Select(c => c.Key).Distinct());

                tasks.Add(MatchEntityService.CombinationUpdateMatchRefinesList(filter));
            }

            var matchRefineCombinationUpdate = await Task.WhenAll(tasks);
            BulkCombinationUpdate bulkMatchRefineUpdate = new BulkCombinationUpdate(MMIv8_Ktype).AddCombinationUpdate((CombinationPipeline<MatchEntity>)matchRefineCombinationUpdate.SelectMany(c => c));

            var bulkMatchRefineResult = await bulkMatchRefineUpdate.CommitBulkWrite();
            Log.Information("Updated Match Refine for {count} matches", bulkMatchRefineResult.ModifiedCount);
        }

        public async Task UpdateFailedFlag(IEnumerable<UpdateFlagRequest> input) // TODO Update to reflect UpdatePreviousMatchedFlag
        {
            var filterBuilder = Builders<MatchEntity>.Filter;
            var filter = filterBuilder.Empty;

            List<Task<IEnumerable<CombinationPipeline<MatchEntity>>>> tasks = new();

            foreach (var inputBatch in input.ToDictionary(c => c.MMI_V8_Key, c => c).Chunk(1000))
            {
                BulkCombinationUpdate bulkCombinationUpdate = new(MMIv8_Ktype);

                foreach (var matches in inputBatch.Select(c => c.Value))
                {
                    filter = filterBuilder.Eq(c => c.TecDocEntity.ExternalId, matches.KTypNr)
                                    & filterBuilder.Eq(c => c.MMIv8Entity.ExternalId, matches.MMI_V8_Key);

                    CombinationPipeline<MatchEntity> combinationUpdate = new(MatchEntityService.Collection, filter);
                    combinationUpdate.AppendUpdate(c => c.SetFailedFlag(matches.Flag))
                                     .AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Checked, $"Updated Failed Flag {matches.Detail}")));

                    bulkCombinationUpdate.AddCombinationUpdate(combinationUpdate);
                }

                var bulkResult = await bulkCombinationUpdate.CommitBulkWrite();
                Log.Information("Updated Failed Flag for {count} matches", bulkResult.ModifiedCount);

                var updateMatchRefineFilter = filterBuilder.In(c => c.MMIv8Entity.ExternalId, inputBatch.Select(c => c.Key).Distinct());

                tasks.Add(MatchEntityService.CombinationUpdateMatchRefinesList(filter));
            }

            var matchRefineCombinationUpdate = await Task.WhenAll(tasks);
            BulkCombinationUpdate bulkMatchRefineUpdate = new BulkCombinationUpdate(MMIv8_Ktype).AddCombinationUpdate((CombinationPipeline<MatchEntity>)matchRefineCombinationUpdate.SelectMany(c => c));

            var bulkMatchRefineResult = await bulkMatchRefineUpdate.CommitBulkWrite();
            Log.Information("Updated Match Refine for {count} matches", bulkMatchRefineResult.ModifiedCount);
        }

        public async Task UpdateMatchRefineStatus(IEnumerable<int> mmi_V8_Keys)
        {
            List<Task<ClientBulkWriteResult>> tasks = new();
            foreach (var mmiEntityID in mmi_V8_Keys.Chunk(1000))
            { 
                var filter = Builders<MatchEntity>.Filter.In(c => c.MMIv8Entity.ExternalId, mmiEntityID);
                var matchEntityUpdate = new CombinationPipeline<MatchEntity>(MatchEntityService.Collection, filter).AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Checked, $"Checked Match Refine")));
                var matchEntityResult = await matchEntityUpdate.UpdateDocuments();
                Log.Information("Updated Status for {count} matches", matchEntityResult?.ModifiedCount);

                var matchRefineUpdate = await MatchEntityService.BulkCombinationUpdateMatchRefine(mmi_V8_Keys);
                tasks.Add(matchRefineUpdate.CommitBulkWrite());
            }

            foreach(var matchRefineResult in tasks)
                Log.Information("Updated Match Refine for {count} matches", (await matchRefineResult).ModifiedCount);
        }

        #endregion

    }
}
