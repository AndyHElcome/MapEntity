using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Rewrite;
using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Core.Endpoints;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Core.Services.Source;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Outputs;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Version = MMIv8_Ktype.Models.Collections.Version;

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
                                VersionService VersionService,
                                EntityRelationService EntityRelationService,
                                UserService UserService,
                                IVersionProvider versionProvider)
    {
        #region Match Make Model
        [Obsolete("Not in use?")]
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

                createdMakeModels.Add(newMatchMakeModel.DocumentId);
                await MatchMakeModelService.Create(newMatchMakeModel);

                if (newMatchMakeModel.TecDocModel.DocumentId is not null && newMatchMakeModel.MMIv8Model.DocumentId is not null)
                {
                    tasks.Add(StoreEntityMatch(newMatchMakeModel));
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
                return existingModelMatch.DocumentId;
            }

            MatchMakeModel newMatchMakeModel = new(
                tecDocModel: matchMakeModel.TD_SourceEntityModelHash == string.Empty ? new() : await SourceTecDocEntityModelService.GetById(matchMakeModel.TD_SourceEntityModelHash) ?? new(),
                mmiv8Model: matchMakeModel.MMI_SourceEntityModelHash == string.Empty ? new() : await SourceMMIv8EntityModelService.GetById(matchMakeModel.MMI_SourceEntityModelHash) ?? new(),
                versionProvider
                );

            await MatchMakeModelService.Create(newMatchMakeModel);

            if (newMatchMakeModel.TecDocModel.DocumentId is not null && newMatchMakeModel.MMIv8Model.DocumentId is not null)
            {
                await StoreEntityMatch(newMatchMakeModel);
            }

            return newMatchMakeModel.DocumentId;
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
                var filter = filterBuilder.Eq(c => c.MatchMakeModelMatchID, modelMatch.DocumentId);

                await MatchEntityService.DeleteByFilter(filter);
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
            var filter = filterBuilder.Eq(c => c.MatchMakeModelMatchID, modelMatch.DocumentId);

            await MatchEntityService.DeleteByFilter(filter);

            return modelMatch.DocumentId;
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
            var filter = filterBuilder.Eq(c => c.MatchMakeModelMatchID, modelMatch.DocumentId);

            await MatchEntityService.DeleteByFilter(filter);

            return modelMatch.DocumentId;
        }
        #endregion

        #region Match Base
        public async Task UpdateMatchScore(MatchBaseType matchBaseType, string matchHash, decimal newScore) // could be endpoint?
        {
            try
            {
                var sw = Stopwatch.StartNew(); 

                var updateMatch = await MatchBaseService.GetById(matchHash) ?? throw new Exception("Match does not exist");

                if (updateMatch.Score != newScore)
                {
                    CombinationPipeline<MatchBase> matchBaseUpdate = MatchBaseService.UpdateScore(updateMatch, newScore);
                    updateMatch = await matchBaseUpdate.FindAndUpdateDocument();

                    CombinationPipeline<MatchEntity> matchEntityUpdate = MatchEntityService.UpdateMatchBaseScoreMatchResult(updateMatch); //TODO this might need to be recalculate
                    var matchEntityResult = await matchEntityUpdate.UpdateDocuments();
                    Log.Debug("Match Entities {time}", sw);

                    BulkCombinationUpdate matchRefineUpdate = await MatchEntityService.BulkCombinationUpdateMatchRefine(matchEntityUpdate.Filter);
                    var matchRefineResult = await matchRefineUpdate.CommitBulkWrite();
                    Log.Debug("Match Refine {time}", sw);

                    sw.Stop();
                    Log.Information("Updated {updateMatch} in {time} ({matchEntitiesCount} MatchEntities) ({matchRefineCount} MatchRefine) ", updateMatch.ToString(), sw, matchEntityResult.IsAcknowledged ? matchEntityResult.ModifiedCount : "notAcknowledged", matchRefineResult.Acknowledged ? matchRefineResult.ModifiedCount : "notAcknowledged");

                }
                else if (updateMatch.Status.Current.Status == Status.Check) //Move this into a update status call
                {
                    CombinationPipeline<MatchBase> matchBaseUpdate = MatchBaseService.Update(updateMatch).AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Checked)));
                    var matchBaseResult = await matchBaseUpdate.UpdateDocuments();

                    sw.Stop();
                    Log.Information("Updated {updateMatch} Status to Checked in {time}", updateMatch.ToString(), matchBaseResult?.ModifiedCount ?? 0, sw);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Cannot update score match {matchBaseType} {matchHash}", matchBaseType, matchHash);
            }
        }

        //public async Task CheckMatchBaseDeprecated()//TODO Utilise Deprecate Match Base
        //{
        //    var currentMatch = await MatchBaseService.GetFindFluent(batchSize: 1000).ToCursorAsync();

        //    while (await currentMatch.MoveNextAsync())
        //    {
        //        foreach (var matchBase in currentMatch.Current)
        //        {
        //            await DeprecateMatchBase(matchBase);
        //        }
        //    }
        //}

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
                _ = await MatchBaseService.Update(matchBase).AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Deprecated))).UpdateDocuments();
            else
                _ = await MatchBaseService.Update(matchBase).AppendPipeline(c => c.RemoveStatus(Status.Deprecated)).UpdateDocuments();

            return isMissing;
        }

        public async Task RecalculateMatchBase(FilterDefinition<MatchEntity> filter, MatchBaseType matchBaseType)
        {
            Log.Debug($"Gathering MatchBases for Recalculation");

            var matchEntityManual = await MatchEntityService.GetMatchBaseByType(matchBaseType, [ MatchBaseMethod.Manual, MatchBaseMethod.Partial ], null, false, filter);
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
                var matchEntityManual = await MatchEntityService.GetMatchBaseByType(matchBaseType, [ MatchBaseMethod.Manual, MatchBaseMethod.Partial ], null, false, filter);
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
                var matchEntityManual = await MatchEntityService.GetMatchBaseByType(matchBaseType, [ MatchBaseMethod.Manual ], null, false);
                var matchBasePartial = await MatchBaseService.GetByTypeAndMethod(matchBaseType, MatchBaseMethod.Partial, Builders<MatchBase>.Filter.Ne(x => x.Status.Current.Status, Status.Deprecated));
                matchBaseDict.Add(matchBaseType, [ .. matchEntityManual.ToList(), .. matchBasePartial ]);

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

                var existingMatchBases = await MatchBaseService.GetByMatchBaseType(groupedMatchBase.Key);

                existingMatchBases = existingMatchBases.IntersectBy(groupedMatchBase.Value.Select(c => c.DocumentId), c => c.DocumentId).ToList();
                var newMatchBases = groupedMatchBase.Value.Where(c => c.MatchBaseMethod == MatchBaseMethod.Manual).ExceptBy(existingMatchBases.Select(c => c.DocumentId), c => c.DocumentId);

                if (existingMatchBases.Any())
                {
                    List<BulkWriteModel> bulks = new();
                    foreach (var existingMatchBase in existingMatchBases)
                    {
                        var newMatchBase = existingMatchBase;
                        if (existingMatchBase.Status.Current.Status == Status.Deprecated)
                        {
                            var matchBaseUpdate = MatchBaseService.Update(existingMatchBase).AppendPipeline(c => c.RemoveStatus(Status.Deprecated));
                            var matchBaseResult = await matchBaseUpdate.UpdateDocuments();

                            newMatchBase = await MatchBaseService.GetById(existingMatchBase.DocumentId) ?? existingMatchBase;
                        }

                        CombinationPipeline<MatchEntity> matchEntityUpdate = MatchEntityService.UpdateMissingMatchBase(newMatchBase, filter);
                        var matchEntityUpdateResult = await matchEntityUpdate.UpdateDocuments();                                             
                    }
                }

                if (newMatchBases.Any())
                {
                    await MatchBaseService.Create(newMatchBases.ToArray());
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

            Log.Information("Updated Match Refine for {count} matches {time}", bulkMatchRefineResult.Acknowledged ? bulkMatchRefineResult.ModifiedCount : "notAcknowledged", sw);
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

        public async Task StorePartialMatchBase(MatchBaseType matchBaseType, string matchHash, decimal? newScore = null) // could be end point
        {
            var matchEntityPartials = await MatchEntityService.GetMatchBaseByType(matchBaseType, [ MatchBaseMethod.Partial ], matchHash);

            var matchEntityPartial = matchEntityPartials.ToList().FirstOrDefault();

            if (matchEntityPartial is null)
                return;

            await MatchBaseService.CreateAndValidate(matchEntityPartial);

            if (newScore is not null)
                await UpdateMatchScore(matchBaseType, matchHash, newScore ?? 0);
        }

        public async Task RemovePartialMatchBase(MatchBaseType matchBaseType, string matchHash) // could be end point
        {
            var matchEntityPartial = await MatchBaseService.GetById(matchHash);

            if (matchEntityPartial is null)
                return;


            await UpdateMatchScore(matchBaseType, matchHash, matchEntityPartial.Reset(versionProvider).Score);

            await MatchBaseService.DeleteById(matchHash);
        }
        #endregion

        #region Match Entity
        public async Task<MatchEntity> CheckMatchVadlidity(int KtypNr, int MMI_V8_Key)
        {
            var currentMatch = await MatchEntityService.GetByExternalIds(KtypNr, MMI_V8_Key);
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
                makeModelMatchID = makeModelMatch.DocumentId;
            }

            var newMatch = new MatchEntity(versionProvider, tecdocEntity, mmiEntity, makeModelMatchID);

            foreach (var matchBase in newMatch.EntityComparison.Values.Where(c => c.MatchBaseMethod == MatchBaseMethod.Manual || c.MatchBaseMethod == MatchBaseMethod.Partial))
            {
                var currentMatchBase = await MatchBaseService.GetById(matchBase.DocumentId);

                if (currentMatchBase is null)
                    continue;
                
                newMatch.EntityComparison[ matchBase.MatchBaseType ] = currentMatchBase;
            }

            newMatch.UpdateScore();

            return newMatch;
        }

        public async Task BulkCreateEntityMatch(List<MatchEntity> newMatches)
        {
            var sw = Stopwatch.StartNew();

            var newMatchesList = newMatches.Select(c => (c.TecDocEntity.KTypNr, c.MMIv8Entity.MMI_V8_Key)).ToList();
            Task<List<EntityRelation>?> checkTask = CheckPreviousMatchedFlag(newMatchesList);

            await MatchEntityService.Create([ .. newMatches ]);

            var checkedEntityRelations = await checkTask;
            if (checkedEntityRelations is not null)
            { 
                var bulk = MatchEntityService.BulkCombinationUpdatePreviousMatchedFlag(checkedEntityRelations);
                var bulkresult = await bulk.CommitBulkWrite();

                sw.Stop();
                Log.Information("Bulk Created {created} with {count} from Previous Match in {time}", newMatches.Count, bulkresult.Acknowledged ? bulkresult.MatchedCount : "0", sw);
            }
            else
            {
                sw.Stop();
                Log.Information("Bulk Created {created} in {time} (no previous match found)", newMatches.Count, sw);
            }
        }

        public async Task StoreEntityMatch(MatchMakeModel makeModelMatch) // memory heavy
        {
            var sw = Stopwatch.StartNew();

            List<Task> createTasks = new();
            using (var tecdocEntities = await SourceTecDocPCService.GetByModelId(makeModelMatch.TecDocModel.DocumentId))
            using (var mmiEntities = await SourceMMIv8Service.GetByModelId(makeModelMatch.MMIv8Model.DocumentId))
            {
                var newMatches = GenerateEntityMatch(await tecdocEntities.ToListAsync(), await mmiEntities.ToListAsync(), makeModelMatch.DocumentId);

                await foreach (var match in newMatches)
                {
                    if (match.Any())
                        createTasks.Add(this.BulkCreateEntityMatch(match.ToList()));
                }
            }

            Task.WaitAll(createTasks.ToArray());

            var filter = Builders<MatchEntity>.Filter.Eq(c => c.MatchMakeModelMatchID, makeModelMatch.DocumentId);
            await this.RecalculateMatchBase(filter);
        }

        public async IAsyncEnumerable<IEnumerable<MatchEntity>> GenerateEntityMatch(IEnumerable<SourceTecDocPC> tecdocEntities, IEnumerable<SourceMMIv8> mmiEntities, ObjectId MakeModelMatchID)
        {
            foreach (var tecdocEntity in tecdocEntities)
            {
                var query = mmiEntities.AsParallel().Select(m => new MatchEntity(versionProvider, tecdocEntity, m, MakeModelMatchID)).Where(m => m.DateIntersection.date_Intersection != 0);

                yield return query.ToList();
            }
        }

        public async Task UpdateFailedFlag(UpdateFlagRequest request)
        {
            var sw = Stopwatch.StartNew();

            var filterBuilder = Builders<MatchEntity>.Filter;
            var filter = filterBuilder.Eq(c => c.TecDocEntity.ExternalId, request.KTypNr)
                       & filterBuilder.Eq(c => c.MMIv8Entity.ExternalId, request.MMI_V8_Key);

            var combinationFlagUpdate =
                new CombinationPipeline<MatchEntity>(MatchEntityService.Collection, filter).AppendUpdate(c => c.SetFailedFlag(request.Flag))
                                                                                           .AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Checked, $"Updated Failed Flag {request.Detail}")));
            var combinationFlagResult = await combinationFlagUpdate.UpdateDocuments();

            var bulkMatchRefineUpdate = await MatchEntityService.BulkCombinationUpdateMatchRefine([ request.MMI_V8_Key ]);
            var bulkMatchRefineResult = await bulkMatchRefineUpdate.CommitBulkWrite();

            sw.Stop();
            Log.Information("Updated Failed Flag {flagUpdate}; {count} MatchRefines in {time}", combinationFlagResult.IsAcknowledged ? combinationFlagResult.ModifiedCount : "notAcknowledged", bulkMatchRefineResult.Acknowledged ? bulkMatchRefineResult.ModifiedCount : "notAcknowledged", sw);
        }

        public async Task UpdateMatchedFlag(UpdateFlagRequest request)
        {
            var sw = Stopwatch.StartNew();
            var filterBuilder = Builders<MatchEntity>.Filter;
            var filter = filterBuilder.Eq(c => c.TecDocEntity.ExternalId, request.KTypNr)
                       & filterBuilder.Eq(c => c.MMIv8Entity.ExternalId, request.MMI_V8_Key);

            var combinationFlagUpdate =
                new CombinationPipeline<MatchEntity>(MatchEntityService.Collection, filter).AppendUpdate(c => c.SetMatchedFlag(request.Flag))
                                                                                           .AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Checked, $"Updated Match Flag {request.Detail}")));
            var combinationFlagResult = await combinationFlagUpdate.UpdateDocuments();

            var bulkMatchRefineUpdate = await MatchEntityService.BulkCombinationUpdateMatchRefine([request.MMI_V8_Key]);
            var bulkMatchRefineResult = await bulkMatchRefineUpdate.CommitBulkWrite();

            sw.Stop();
            Log.Information("Updated Match Flag {flagUpdate}; {count} MatchRefines in {time}", combinationFlagResult.IsAcknowledged ? combinationFlagResult.ModifiedCount : "notAcknowledged", bulkMatchRefineResult.Acknowledged ? bulkMatchRefineResult.ModifiedCount : "notAcknowledged", sw);
        }

        public async Task UpdateMatchRefineStatus(int mmi_V8_Key)
        {
            var sw = Stopwatch.StartNew();
            var filter = Builders<MatchEntity>.Filter.Eq(c => c.MMIv8Entity.ExternalId, mmi_V8_Key)
                       & Builders<MatchEntity>.Filter.Eq(c => c.Status.Current.Status, Status.Check);

            var matchEntityUpdate = new CombinationPipeline<MatchEntity>(MatchEntityService.Collection, filter).AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Checked, $"Checked Match Refine")));
            var matchEntityResult = await matchEntityUpdate.UpdateDocuments();

            var bulkMatchRefineUpdate = await MatchEntityService.BulkCombinationUpdateMatchRefine([ mmi_V8_Key ]);
            var bulkMatchRefineResult = await bulkMatchRefineUpdate.CommitBulkWrite();

            sw.Stop();
            Log.Information("Updated Checked Status {matchEntityCount} MatchEntities; {matchRefineCount} MatchRefines in {time}", matchEntityResult.IsAcknowledged ? matchEntityResult.ModifiedCount : "notAcknowledged", bulkMatchRefineResult.Acknowledged ? bulkMatchRefineResult.ModifiedCount : "notAcknowledged", sw);
        }

        public async Task ResetMatchResult(int mmi_V8_Key)
        {
            var sw = Stopwatch.StartNew();
            var filter = Builders<MatchEntity>.Filter.Eq(c => c.MMIv8Entity.ExternalId, mmi_V8_Key);

            var matchEntityUpdate = new CombinationPipeline<MatchEntity>(MatchEntityService.Collection, filter).AppendUpdate(c => c.SetMatchedFlag(false, ""))
                                                                                                               .AppendPipeline(c => c.UpdateScoreMatchResult())
                                                                                                               .AppendPipeline(c => c.RemoveStatus(Status.Checked));
            var matchEntityResult = await matchEntityUpdate.UpdateDocuments();

            var bulkMatchRefineUpdate = await MatchEntityService.BulkCombinationUpdateMatchRefine([ mmi_V8_Key ]);
            var bulkMatchRefineResult = await bulkMatchRefineUpdate.CommitBulkWrite();

            sw.Stop();
            Log.Information("Updated Checked Status {matchEntityCount} MatchEntities; {matchRefineCount} MatchRefines in {time}", matchEntityResult.IsAcknowledged ? matchEntityResult.ModifiedCount : "notAcknowledged", bulkMatchRefineResult.Acknowledged ? bulkMatchRefineResult.ModifiedCount : "notAcknowledged", sw);
        }
        #endregion

        #region Version
        public async Task<Result<Version>> CreateVersion(string tecdocEntityVersion, string mmiv8EntityVersion, string userName)
        {
            var userResult = await UserService.GetByName(userName);
            if (!userResult.IsSuccess)
                return userResult.Error!;

            return await VersionService.Create(tecdocEntityVersion, mmiv8EntityVersion, userResult.Value);
        }

        public async Task<Result<Version>> UpdateVersion(int versionNumber, string? tecdocEntityVersion = null, string? mmiv8EntityVersion = null, string? userName = null)
        {
            var versionResult = await VersionService.GetByVersion(versionNumber);
            if (!versionResult.IsSuccess)
                return versionResult;

            User? user = null;
            if (userName is not null)
            {
                var userResult = await UserService.GetByName(userName);
                if (!userResult.IsSuccess)
                    return userResult.Error!;
                user = userResult.Value;
            }

            var versionUpdate = VersionService.Update(versionResult.Value).AppendUpdate(c => c.UpdateVersionTecdocEntityVersion(tecdocEntityVersion)
                                                                                              .UpdateVersionMMIv8EntityVersion(mmiv8EntityVersion)
                                                                                              .UpdateVersionUser(user));

            return await versionUpdate.FindAndUpdateDocument();
        }
        #endregion

        #region EntityRelation
        public async Task CreateEntityRelation(int versionNumber, List<PutEntityRelationRequest> entityRelationsRequest) //TODO Change this to allow not updating the Matchentities if not last version // This could be an endpoint
        {
            var sw = Stopwatch.StartNew();

            var versionResult = await VersionService.GetByVersion(versionNumber);
            var entityRelations = entityRelationsRequest.ConvertAll(c => new EntityRelation(versionResult.Value.DocumentId, c.MMI_V8_Key, c.KTypNr, c.Comment, c.VersionNumber));

            await EntityRelationService.Create([ .. entityRelations ]);

            var bulkPreviousFlagUpdate = MatchEntityService.BulkCombinationUpdatePreviousMatchedFlag(entityRelations);
            var bulkPreviousFlagResult = await bulkPreviousFlagUpdate.CommitBulkWrite();

            var bulkMatchRefineUpdate = await MatchEntityService.BulkCombinationUpdateMatchRefine(entityRelations.Select(c => c.MMI_V8_Key).Distinct());
            var bulkMatchRefineResult = await bulkMatchRefineUpdate.CommitBulkWrite();

            sw.Stop();
            Log.Information("Created {entityRelationCount} EntityRelations; Updated MatchEntities: {previousFlagCount} Previous Flags; {count} MatchRefines in {time}", entityRelations.Count, bulkPreviousFlagResult.Acknowledged ? bulkPreviousFlagResult.ModifiedCount : "notAcknowledged", bulkMatchRefineResult.Acknowledged ? bulkMatchRefineResult.ModifiedCount : "notAcknowledged", sw);
        }

        public async Task DeleteEntityRelation(List<EntityRelation> entityRelations) //TODO Change this to allow not updating the Matchentities if not last version // This could be an endpoint
        {
            var sw = Stopwatch.StartNew();

            await EntityRelationService.Delete([ .. entityRelations ]);

            var bulkPreviousFlagUpdate = MatchEntityService.BulkCombinationUpdatePreviousMatchedFlag(entityRelations, false);
            var bulkPreviousFlagResult = await bulkPreviousFlagUpdate.CommitBulkWrite();

            var bulkMatchRefineUpdate = await MatchEntityService.BulkCombinationUpdateMatchRefine(entityRelations.Select(c => c.MMI_V8_Key));
            var bulkMatchRefineResult = await bulkMatchRefineUpdate.CommitBulkWrite();

            sw.Stop();
            Log.Information("Deleted {entityRelationCount} EntityRelations; Updated MatchEntities: {previousFlagCount} Previous Flags; {count} MatchRefines in {time}", entityRelations.Count, bulkPreviousFlagResult.Acknowledged ? bulkPreviousFlagResult.ModifiedCount : "notAcknowledged", bulkMatchRefineResult.Acknowledged ? bulkMatchRefineResult.ModifiedCount : "notAcknowledged", sw);
        }

        public async Task<Version> GetPreviousVersionIDWithEntityRelations()
        {
            var entityRelationVersionIDs = await EntityRelationService.GetQueryable().Select(c => c.VersionID).Distinct().ToListAsync();

            var currentVersionIDResult = await VersionService.GetCurrentVersion();

            if (!currentVersionIDResult.IsSuccess)
                return null;

            Version version = await VersionService.GetQueryable()
                                                  .Where(c => entityRelationVersionIDs.Contains(c.DocumentId) && c.DocumentId != currentVersionIDResult.Value.DocumentId)
                                                  .OrderByDescending(c => c.VersionNumber)
                                                  .FirstOrDefaultAsync();
            return version;
        }

        public async Task<List<EntityRelation>?> CheckPreviousMatchedFlag(List<(int KTypNr, int MMI_V8_Key)> entityRelations)
        {
            var previousVersion = await GetPreviousVersionIDWithEntityRelations();

            if (previousVersion is null)
                return null;
                //throw new Exception("Exception with CheckPreviousMatchedFlag previousVersion is null");

            var query = EntityRelationService.GetQueryable().Where(c => c.VersionID == previousVersion.DocumentId && entityRelations.Contains(new(c.KTypNr, c.MMI_V8_Key)));

            return await query.ToListAsync();
        }
        #endregion

        #region User
        public async Task<Result<DeleteResult>> DeleteUser(string userName)
        {
            var user = await UserService.GetByName(userName);
            if (!user.IsSuccess)
                return user.Error!;

            var versionResult = await VersionService.GetByUser(userName);

            if (versionResult.IsSuccess && versionResult.Value.Count != 0)
                return Error.Validation("User.DeletionValidation", $"Cannot Delete user {userName} as it's in use");
            
            if (!versionResult.IsSuccess && versionResult.Error!.Type == ErrorType.NoContent)
                return await UserService.DeleteByName(userName);
                
            throw new NotImplementedException();
        }
        #endregion
    }
}
