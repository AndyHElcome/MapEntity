using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
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
using System.Xml;
using Version = MMIv8_Ktype.Models.Collections.Version;

namespace MMIv8_Ktype.Core.Services.Mapping
{
    public class MappingService(MatchMakeModelService MatchMakeModelService,
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
        public async Task<Result> CreateMakeModelMatch(string TD_SourceEntityModelHash, string MMI_SourceEntityModelHash)
        {
            MongoSourceEntityModel tecdocModel = new();
            MongoSourceEntityModel mmiv8Model = new();

            if (TD_SourceEntityModelHash != string.Empty && await SourceTecDocEntityModelService.GetById(TD_SourceEntityModelHash) is var tecdocSourceEntityModelResult && tecdocSourceEntityModelResult.IsSuccess)
                tecdocModel = tecdocSourceEntityModelResult.Value;

            if (MMI_SourceEntityModelHash != string.Empty && await SourceMMIv8EntityModelService.GetById(MMI_SourceEntityModelHash) is var mmiv8SourceEntityModelResult && mmiv8SourceEntityModelResult.IsSuccess)
                mmiv8Model = mmiv8SourceEntityModelResult.Value;

            if (tecdocModel.DocumentId is null && mmiv8Model.DocumentId is null)
                return Error.NotFound("MatchMatchModel.EntitiesNotFound", $"No SourceEntityModels found for either \"{@TD_SourceEntityModelHash}\" ({tecdocModel is not null}) or \"{@MMI_SourceEntityModelHash}\" ({mmiv8Model is not null})");

            MatchMakeModel newMatchMakeModel = new(tecdocModel, mmiv8Model, versionProvider);

            var createResult = await MatchMakeModelService.Create(newMatchMakeModel);
            if (!createResult.IsSuccess)
                return createResult;

            if (newMatchMakeModel is { TecDocModel: { DocumentId: not null }, MMIv8Model: { DocumentId: not null } })
                return await StoreEntityMatch(newMatchMakeModel);
            if (newMatchMakeModel.TecDocModel.DocumentId is not null && newMatchMakeModel.MMIv8Model.DocumentId is not null)
                throw new Exception("Bad pattern matching");


            return Result.Success();
        }

        public async Task<Result> DeleteMakeModelMatch(ObjectId documentId)
        {
            var deleteMatchMakeModel = await MatchMakeModelService.DeleteById(documentId);
            if (!deleteMatchMakeModel.IsSuccess)
                return deleteMatchMakeModel;

            var filter = Builders<MatchEntity>.Filter.Eq(c => c.MatchMakeModelMatchID, deleteMatchMakeModel.Value.DocumentId);
            var deleteMatchEntityResult = await MatchEntityService.DeleteByFilter(filter);
            if (!deleteMatchEntityResult.IsSuccess)
                return deleteMatchEntityResult;

            return deleteMatchMakeModel;
        }
        #endregion

        #region Match Base
        public async Task<Result> UpdateMatchScore(string matchHash, decimal newScore, bool force = false) // could be endpoint?
        {
            var sw = Stopwatch.StartNew();

            var matchBaseResult = await MatchBaseService.GetById(matchHash);
            if (!matchBaseResult.IsSuccess)
                return matchBaseResult.Error!;

            if (matchBaseResult.Value.Score == newScore && !force)
                return Error.Validation("MatchBase.UpdateScoreValidation", "No change in score, nothing to update");

            var updateMatchResult = await MatchBaseService.CombinationUpdateMatchBaseScore(matchBaseResult.Value, newScore).FindAndUpdateDocument();
            if (!updateMatchResult.IsSuccess)
                return updateMatchResult;

            FilterDefinition<MatchEntity> filter;
            string matchEntityResultString;
            if (updateMatchResult.Value.MatchContexts is null)
            {
                CombinationPipeline<MatchEntity> matchEntityUpdate = MatchEntityService.UpdateMatchBaseScoreMatchResult(updateMatchResult.Value); //TODO this might need to be recalculate
                var matchEntityResult = await matchEntityUpdate.UpdateDocuments();
                Log.Debug("Match Entities {time}", sw);
                if (!matchEntityResult.IsSuccess)
                    return matchEntityResult;

                filter = matchEntityUpdate.Filter;
                matchEntityResultString =  matchEntityResult.Value.IsAcknowledged ? matchEntityResult.Value.ModifiedCount.ToString() : "notAcknowledged";
            }
            else
            {
                BulkCombinationUpdate matchEntityUpdate = MatchEntityService.UpdateMatchBaseScoreMatchResultWithContexts(updateMatchResult.Value, out filter); //TODO this might need to be recalculate
                var matchEntityResult = await matchEntityUpdate.CommitBulkWrite();
                Log.Debug("Match Entities with Contexts {time}", sw);
                if (!matchEntityResult.IsSuccess)
                    return matchEntityResult;

                matchEntityResultString = matchEntityResult.Value.Acknowledged ? matchEntityResult.Value.ModifiedCount.ToString() : "notAcknowledged";
            }

            BulkCombinationUpdate matchRefineUpdate = await MatchEntityService.BulkCombinationUpdateMatchRefine(filter);
            var matchRefineResult = await matchRefineUpdate.CommitBulkWrite();
            Log.Debug("Match Refine {time}", sw);
            if (!matchRefineResult.IsSuccess)
                return matchRefineResult;

            sw.Stop();
            Log.Information("Updated {updateMatch} in {time} ({matchEntitiesCount} MatchEntities) ({matchRefineCount} MatchRefine) ", updateMatchResult.Value.ToString(), sw, matchEntityResultString, matchRefineResult.Value.Acknowledged ? matchRefineResult.Value.ModifiedCount : "notAcknowledged");

            return Result.Success();
        }

        public async Task<Result<UpdateResult>> AddMatchContext(string documentId, MatchContext matchContext)
        {
            var matchBaseResult = await MatchBaseService.GetById(documentId);
            if (!matchBaseResult.IsSuccess)
                return matchBaseResult.Error!;

            matchBaseResult.Value.MatchContexts = new();
            var currentContext = matchBaseResult.Value.MatchContexts?.Find(c => c.ContextId == matchContext.ContextId);

            if (currentContext is not null && currentContext.ScoreOverride == matchContext.ScoreOverride)
                return Error.Validation("MatchBase.MatchContext.ScoreOverrideValidation", "MatchContext ScoreOverride has not changed");

            if (currentContext is not null)
                matchBaseResult.Value.MatchContexts?.RemoveAll(c => c.ContextId == matchContext.ContextId);

            matchBaseResult.Value.MatchContexts.Add(matchContext);

            var combinationUpdateResult = await MatchBaseService.CombinationUpdateMatchContext(matchBaseResult.Value, matchBaseResult.Value.MatchContexts, $"NewContext: {matchContext.ContextId}").UpdateDocuments();
            if (!combinationUpdateResult.IsSuccess)
                return combinationUpdateResult;

            var updateScoreResult = await this.UpdateMatchScore(documentId, matchBaseResult.Value.Score, true);
            if (!updateScoreResult.IsSuccess)
                return updateScoreResult.Error!;

            return combinationUpdateResult;
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
                _ = await MatchBaseService.Update(matchBase).AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Deprecated))).UpdateDocuments();
            else
                _ = await MatchBaseService.Update(matchBase).AppendPipeline(c => c.RemoveStatus(Status.Deprecated)).UpdateDocuments();

            return isMissing;
        }

        public async Task<Result> RecalculateAutomaticMatchBaseScore(FilterDefinition<MatchEntity> filter, MatchBaseType matchBaseType)
        {
            var sw = Stopwatch.StartNew();

            Log.Debug($"Gathering MatchBases for Recalculation");

            var matchBaseAutomatic = await MatchEntityService.GetMatchBaseByType(matchBaseType, [ MatchBaseMethod.Automatic ], null, false, filter);
            if (!matchBaseAutomatic.IsSuccess)
                return matchBaseAutomatic;

            Log.Debug("Retrived {Count} for {MatchBaseType} in {time}", matchBaseAutomatic.Value.Count(), matchBaseType, sw);

            foreach(var matchBase in matchBaseAutomatic.Value)
            {
                sw.Restart();

                CombinationPipeline<MatchEntity> matchEntityUpdate = MatchEntityService.UpdateMatchBaseScoreMatchResult(matchBase.Reset(versionProvider), filter); //TODO this might need to be recalculate
                var matchEntityResult = await matchEntityUpdate.UpdateDocuments();
                Log.Debug("Match Entities {time}", sw);
                if (!matchEntityResult.IsSuccess)
                    return matchEntityResult;

                Log.Information("Updated {updateMatch} in {time} ({matchEntitiesCount} MatchEntities)", matchBase.ToString(), sw, matchEntityResult.Value.IsAcknowledged ? matchEntityResult.Value.ModifiedCount : "notAcknowledged");
            }
            sw.Restart();

            BulkCombinationUpdate matchRefineUpdate = await MatchEntityService.BulkCombinationUpdateMatchRefine(filter);
            var matchRefineResult = await matchRefineUpdate.CommitBulkWrite();
            Log.Debug("Match Refine {time}", sw);
            if (!matchRefineResult.IsSuccess)
                return matchRefineResult;

            sw.Stop();
            Log.Information("Updated {updateMatch} in {time} ({matchEntitiesCount} MatchEntities) ({matchRefineCount} MatchRefine)", matchRefineResult.Value.Acknowledged ? matchRefineResult.Value.ModifiedCount : "notAcknowledged", sw);

            return Result.Success();
        }

        public async Task<Result> RecalculateMatchBase(FilterDefinition<MatchEntity> filter, MatchBaseType matchBaseType)
        {
            Log.Debug($"Gathering MatchBases for Recalculation");

            var matchEntityManual = await MatchEntityService.GetMatchBaseByType(matchBaseType, [ MatchBaseMethod.Manual, MatchBaseMethod.Partial ], null, false, filter);
            if (!matchEntityManual.IsSuccess)
                return matchEntityManual;

            Dictionary<MatchBaseType, IEnumerable<MatchBase>> matchBaseDict = new() { { matchBaseType, matchEntityManual.Value } };

            Log.Debug("Retrived {Count} for {MatchBaseType}", matchBaseDict[ matchBaseType ].Count(), matchBaseType);

            return await CalculateMatchBase(matchBaseDict, filter);
        }

        public async Task<Result> RecalculateMatchBase(FilterDefinition<MatchEntity> filter)
        {
            Log.Debug($"Gathering MatchBases for Recalculation");

            Dictionary<MatchBaseType, IEnumerable<MatchBase>> matchBaseDict = new();
            foreach (MatchBaseType matchBaseType in (MatchBaseType[])Enum.GetValues(typeof(MatchBaseType)))
            {
                var matchEntityManual = await MatchEntityService.GetMatchBaseByType(matchBaseType, [ MatchBaseMethod.Manual, MatchBaseMethod.Partial ], null, false, filter);
                if (matchEntityManual.IsSuccess)
                {
                    matchBaseDict.Add(matchBaseType, matchEntityManual.Value);
                    Log.Debug("Retrived {Count} for {MatchBaseType}", matchBaseDict[ matchBaseType ].Count(), matchBaseType);
                }
            }
            return await CalculateMatchBase(matchBaseDict, filter);
        }

        public async Task<Result> RecalculateMatchBase()
        {
            Log.Debug($"Gathering MatchBases for Recalculation");

            Dictionary<MatchBaseType, IEnumerable<MatchBase>> matchBaseDict = new();
            foreach (MatchBaseType matchBaseType in (MatchBaseType[])Enum.GetValues(typeof(MatchBaseType)))
            {
                var matchEntityManual = await MatchEntityService.GetMatchBaseByType(matchBaseType, [ MatchBaseMethod.Manual ], null, false);
                if (matchEntityManual.IsSuccess)
                    matchBaseDict.Add(matchBaseType, matchEntityManual.Value);

                var matchBasePartialResult = await MatchBaseService.GetByTypeAndMethod(matchBaseType, MatchBaseMethod.Partial, Builders<MatchBase>.Filter.Ne(x => x.Status.Current.Status, Status.Deprecated));
                if (matchBasePartialResult.IsSuccess)
                    matchBaseDict.Add(matchBaseType, matchBasePartialResult.Value);

                if (matchEntityManual.IsSuccess || matchBasePartialResult.IsSuccess)
                    Log.Debug("Retrived {Count} for {MatchBaseType}", matchBaseDict[ matchBaseType ].Count(), matchBaseType);
            }
            return await CalculateMatchBase(matchBaseDict);
        }

        public async Task<Result> CalculateMatchBase(Dictionary<MatchBaseType, IEnumerable<MatchBase>> matchBaseDict, FilterDefinition<MatchEntity>? filter = null)
        {
            filter ??= Builders<MatchEntity>.Filter.Empty;

            var sw = Stopwatch.StartNew();

            foreach (var groupedMatchBase in matchBaseDict.Where(c => c.Value.Any()))
            {
                Log.Debug("Starting {MatchBaseType} {time}", groupedMatchBase.Key, sw);

                List<MatchBase> existingMatchBases;
                var existingMatchBasesResult = await MatchBaseService.GetByMatchBaseType(groupedMatchBase.Key);
                if (existingMatchBasesResult.IsSuccess)
                    existingMatchBases = existingMatchBasesResult.Value.IntersectBy(groupedMatchBase.Value.Select(c => c.DocumentId), c => c.DocumentId).ToList();
                else if (existingMatchBasesResult.Error!.Type == ErrorType.NoContent)
                    existingMatchBases = new();
                else
                    return existingMatchBasesResult;

                if (existingMatchBases is { Count: >0 })
                {
                    List<BulkWriteModel> bulks = new();
                    foreach (var existingMatchBase in existingMatchBases)
                    {
                        var newMatchBase = existingMatchBase;
                        if (existingMatchBase.Status.Current.Status == Status.Deprecated)
                        {
                            var matchBaseUpdate = MatchBaseService.Update(existingMatchBase).AppendPipeline(c => c.RemoveStatus(Status.Deprecated));
                            var newMatchBaseResult = await matchBaseUpdate.FindAndUpdateDocument();
                            newMatchBase = newMatchBaseResult.Value;
                        }

                        CombinationPipeline<MatchEntity> matchEntityUpdate = MatchEntityService.UpdateMissingMatchBase(newMatchBase, filter);
                        var matchEntityUpdateResult = await matchEntityUpdate.UpdateDocuments();                                             
                    }
                }

                var newMatchBases = groupedMatchBase.Value.Where(c => c.MatchBaseMethod == MatchBaseMethod.Manual).ExceptBy(existingMatchBases.Select(c => c.DocumentId), c => c.DocumentId);
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
            if (!validationResult.IsSuccess)
                return validationResult;
            Log.Debug("Finished Revalidation {time}", sw);


            Log.Debug("Starting MatchRefine Update {time}", sw);

            BulkCombinationUpdate bulkMatchRefineUpdate = await MatchEntityService.BulkCombinationUpdateMatchRefine(filter);
            var bulkMatchRefineResult = await bulkMatchRefineUpdate.CommitBulkWrite();
            if (!bulkMatchRefineResult.IsSuccess)
                return bulkMatchRefineResult;

            Log.Information("Updated Match Refine for {count} matches {time}", bulkMatchRefineResult.Value.Acknowledged ? bulkMatchRefineResult.Value.ModifiedCount : "notAcknowledged", sw);

            return Result.Success();
        }

        public async Task<Result> StorePartialMatchBase(MatchBaseType matchBaseType, string matchHash, decimal? newScore = null) // could be end point
        {
            var matchEntityPartialResult = await MatchEntityService.GetMatchBaseByType(matchBaseType, [ MatchBaseMethod.Partial ], matchHash);
            if (!matchEntityPartialResult.IsSuccess)
                return matchEntityPartialResult;

            var matchEntityPartial = matchEntityPartialResult.Value.First();

            var createResult = await MatchBaseService.Create(matchEntityPartial);
            if (!createResult.IsSuccess)
                return createResult;

            if (newScore is not null)
              return await UpdateMatchScore(matchHash, newScore ?? 0);
            else
                return Result.Success();
        }

        public async Task<Result> RemovePartialMatchBase(MatchBaseType matchBaseType, string matchHash) // could be end point
        {
            var matchEntityPartialResult = await MatchBaseService.GetById(matchHash);
            if (!matchEntityPartialResult.IsSuccess)
                return matchEntityPartialResult;

            var updateResult = await UpdateMatchScore(matchHash, matchEntityPartialResult.Value.Reset(versionProvider).Score, true);
            if (!updateResult.IsSuccess)
                return updateResult;

            var deleteResult = await MatchBaseService.DeleteById(matchHash);
            if (!deleteResult.IsSuccess)
                return deleteResult;

            return Result.Success();
        }
        #endregion

        #region Match Entity
        public async Task<Result<MatchEntity>> CheckMatchVadlidity(int KtypNr, int MMI_V8_Key)
        {
            var currentMatch = await MatchEntityService.GetByExternalIds(KtypNr, MMI_V8_Key);
            if (currentMatch.IsSuccess)
                return currentMatch;

            var tecdocEntityResult = await SourceTecDocPCService.GetByExternalId(KtypNr);
            if (!tecdocEntityResult.IsSuccess)
                return tecdocEntityResult.Error!;

            var mmiEntityResult = await SourceMMIv8Service.GetByExternalId(MMI_V8_Key);
            if (!mmiEntityResult.IsSuccess)
                return mmiEntityResult.Error!;

            ObjectId makeModelMatchID = ObjectId.Empty;
            var makeModelMatchResult = await MatchMakeModelService.GetByModelIds(tecdocEntityResult.Value.SourceEntityModelHash, mmiEntityResult.Value.SourceEntityModelHash);

            if (makeModelMatchResult.IsSuccess)
                makeModelMatchID = makeModelMatchResult.Value.DocumentId;
            else if (makeModelMatchResult.Error!.Type == ErrorType.NoContent)
                Log.Warning("Make Model Match doesn't exist");
            else
                return makeModelMatchResult.Error!;

            var newMatch = new MatchEntity(versionProvider, tecdocEntityResult.Value, mmiEntityResult.Value, makeModelMatchID);

            foreach (var matchBase in newMatch.EntityComparison.Values.Where(c => c.MatchBaseMethod == MatchBaseMethod.Manual || c.MatchBaseMethod == MatchBaseMethod.Partial))
            {
                var currentMatchBase = await MatchBaseService.GetById(matchBase.DocumentId);

                if (currentMatchBase.IsSuccess)
                    newMatch.EntityComparison[ matchBase.MatchBaseType ] = currentMatchBase.Value;
            }

            newMatch.UpdateScore();

            return newMatch;
        }

        public async Task<Result> BulkCreateEntityMatch(List<MatchEntity> newMatches)
        {
            var sw = Stopwatch.StartNew();

            var newMatchesList = newMatches.Select(c => (c.TecDocEntity.KTypNr, c.MMIv8Entity.MMI_V8_Key)).ToList();
            Task<Result<List<EntityRelation>>> checkTask = this.CheckPreviousMatchedFlag(newMatchesList);

            var createResult = await MatchEntityService.Create([ .. newMatches ]);
            if (!createResult.IsSuccess)
                return createResult;

            var checkedEntityRelationsResult = await checkTask;
            if (checkedEntityRelationsResult.IsSuccess )
            {
                var bulkCombinationUpdateResult = await MatchEntityService.BulkCombinationUpdatePreviousMatchedFlag(checkedEntityRelationsResult.Value).CommitBulkWrite();

                sw.Stop();
                Log.Information("Bulk Created {created} with {count} from Previous Match in {time}", newMatches.Count, bulkCombinationUpdateResult.Value.Acknowledged ? bulkCombinationUpdateResult.Value.MatchedCount : "0", sw);
                return bulkCombinationUpdateResult;
            }
            else if (checkedEntityRelationsResult.Error.Type == ErrorType.NoContent)
            {
                sw.Stop();
                Log.Information("Bulk Created {created} in {time} (no previous match found)", newMatches.Count, sw);
                return Result.Success();
            }
            else
            { 
                return checkedEntityRelationsResult; 
            }
        }

        public async Task<Result> StoreEntityMatch(MatchMakeModel makeModelMatch) // memory heavy
        {
            var sw = Stopwatch.StartNew();

            List<Task> createTasks = new();
            var tecdocEntities = await SourceTecDocPCService.GetByModelId(makeModelMatch.TecDocModel.DocumentId);
            if (!tecdocEntities.IsSuccess)
                return tecdocEntities;
            
            var mmiEntities = await SourceMMIv8Service.GetByModelId(makeModelMatch.MMIv8Model.DocumentId);
            if (!mmiEntities.IsSuccess)
                return mmiEntities;

            var newMatches = GenerateEntityMatch(tecdocEntities.Value, mmiEntities.Value, makeModelMatch.DocumentId);
            await foreach (var match in newMatches)
            {
                if (match.Any())
                    createTasks.Add(this.BulkCreateEntityMatch(match.ToList()));
            }
            
            Task.WaitAll(createTasks.ToArray());

            var filter = Builders<MatchEntity>.Filter.Eq(c => c.MatchMakeModelMatchID, makeModelMatch.DocumentId);
            await this.RecalculateMatchBase(filter);

            return Result.Success();
        }

        public async IAsyncEnumerable<IEnumerable<MatchEntity>> GenerateEntityMatch(IEnumerable<SourceTecDocPC> tecdocEntities, IEnumerable<SourceMMIv8> mmiEntities, ObjectId MakeModelMatchID)
        {
            foreach (var tecdocEntity in tecdocEntities)
            {
                //var query = mmiEntities.AsParallel().Select(m => new MatchEntity(versionProvider, tecdocEntity, m, MakeModelMatchID)).Where(m => m.DateIntersection.date_Intersection != 0);
                var query = mmiEntities.AsParallel().Select(m => new MatchEntity(versionProvider, tecdocEntity, m, MakeModelMatchID)); //TODO Can't filter out bad dates unless we add them back in when updating 

                yield return query.ToList();
            }
        }

        public async Task<Result> UpdateFailedFlag(UpdateFlagRequest request)
        {
            var sw = Stopwatch.StartNew();
            var filterBuilder = Builders<MatchEntity>.Filter;
            var filter = filterBuilder.Eq(c => c.TecDocEntity.ExternalId, request.KTypNr)
                       & filterBuilder.Eq(c => c.MMIv8Entity.ExternalId, request.MMI_V8_Key);

            var combinationFlagUpdate =
                new CombinationPipeline<MatchEntity>(MatchEntityService.Collection, filter).AppendUpdate(c => c.SetFailedFlag(request.Flag, request.Detail))
                                                                                           .AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Checked, $"Updated Failed Flag {request.Detail}")));
            var combinationFlagResult = await combinationFlagUpdate.UpdateDocuments();
            if (!combinationFlagResult.IsSuccess)
                return combinationFlagResult;

            var bulkMatchRefineUpdate = await MatchEntityService.BulkCombinationUpdateMatchRefine([ request.MMI_V8_Key ]);
            var bulkMatchRefineResult = await bulkMatchRefineUpdate.CommitBulkWrite();
            if (!bulkMatchRefineResult.IsSuccess)
                return bulkMatchRefineResult;

            sw.Stop();
            Log.Information("Updated Failed Flag {flagUpdate}; {count} MatchRefines in {time}", combinationFlagResult.Value.IsAcknowledged ? combinationFlagResult.Value.ModifiedCount : "notAcknowledged", bulkMatchRefineResult.Value.Acknowledged ? bulkMatchRefineResult.Value.ModifiedCount : "notAcknowledged", sw);

            return Result.Success();
        }

        public async Task<Result> UpdateMatchedFlag(UpdateFlagRequest request)
        {
            var sw = Stopwatch.StartNew();
            var filterBuilder = Builders<MatchEntity>.Filter;
            var filter = filterBuilder.Eq(c => c.TecDocEntity.ExternalId, request.KTypNr)
                       & filterBuilder.Eq(c => c.MMIv8Entity.ExternalId, request.MMI_V8_Key);

            var combinationFlagUpdate =
                new CombinationPipeline<MatchEntity>(MatchEntityService.Collection, filter).AppendUpdate(c => c.SetMatchedFlag(request.Flag, request.Detail))
                                                                                           .AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Checked, $"Updated Match Flag {request.Detail}")));
            var combinationFlagResult = await combinationFlagUpdate.UpdateDocuments();
            if (!combinationFlagResult.IsSuccess)
                return combinationFlagResult;

            var bulkMatchRefineUpdate = await MatchEntityService.BulkCombinationUpdateMatchRefine([request.MMI_V8_Key]);
            var bulkMatchRefineResult = await bulkMatchRefineUpdate.CommitBulkWrite();
            if (!bulkMatchRefineResult.IsSuccess)
                return bulkMatchRefineResult;

            sw.Stop();
            Log.Information("Updated Match Flag {flagUpdate}; {count} MatchRefines in {time}", combinationFlagResult.Value.IsAcknowledged ? combinationFlagResult.Value.ModifiedCount : "notAcknowledged", bulkMatchRefineResult.Value.Acknowledged ? bulkMatchRefineResult.Value.ModifiedCount : "notAcknowledged", sw);

            return Result.Success();
        }

        public async Task<Result> UpdateMatchRefineStatus(int mmi_V8_Key)
        {
            var sw = Stopwatch.StartNew();
            var filter = Builders<MatchEntity>.Filter.Eq(c => c.MMIv8Entity.ExternalId, mmi_V8_Key)
                       & Builders<MatchEntity>.Filter.Eq(c => c.Status.Current.Status, Status.Check);

            var matchEntityUpdate = new CombinationPipeline<MatchEntity>(MatchEntityService.Collection, filter).AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Checked, $"Checked Match Refine")));
            var matchEntityResult = await matchEntityUpdate.UpdateDocuments();
            if (!matchEntityResult.IsSuccess)
                return matchEntityResult;

            var bulkMatchRefineUpdate = await MatchEntityService.BulkCombinationUpdateMatchRefine([ mmi_V8_Key ]);
            var bulkMatchRefineResult = await bulkMatchRefineUpdate.CommitBulkWrite();
            if (!bulkMatchRefineResult.IsSuccess)
                return bulkMatchRefineResult;

            sw.Stop();
            Log.Information("Updated Checked Status {matchEntityCount} MatchEntities; {matchRefineCount} MatchRefines in {time}", matchEntityResult.Value.IsAcknowledged ? matchEntityResult.Value.ModifiedCount : "notAcknowledged", bulkMatchRefineResult.Value.Acknowledged ? bulkMatchRefineResult.Value.ModifiedCount : "notAcknowledged", sw);

            return Result.Success();
        }

        public async Task<Result> ResetMatchResult(int mmi_V8_Key)
        {
            var sw = Stopwatch.StartNew();
            var filter = Builders<MatchEntity>.Filter.Eq(c => c.MMIv8Entity.ExternalId, mmi_V8_Key);

            var matchEntityUpdate = new CombinationPipeline<MatchEntity>(MatchEntityService.Collection, filter).AppendUpdate(c => c.SetMatchedFlag(false, ""))
                                                                                                               .AppendPipeline(c => c.UpdateScoreMatchResult())
                                                                                                               .AppendPipeline(c => c.RemoveStatus(Status.Checked));
            var matchEntityResult = await matchEntityUpdate.UpdateDocuments();
            if (!matchEntityResult.IsSuccess)
                return matchEntityResult;

            var bulkMatchRefineUpdate = await MatchEntityService.BulkCombinationUpdateMatchRefine([ mmi_V8_Key ]);
            var bulkMatchRefineResult = await bulkMatchRefineUpdate.CommitBulkWrite();
            if (!bulkMatchRefineResult.IsSuccess)
                return bulkMatchRefineResult;

            sw.Stop();
            Log.Information("Updated Checked Status {matchEntityCount} MatchEntities; {matchRefineCount} MatchRefines in {time}", matchEntityResult.Value.IsAcknowledged ? matchEntityResult.Value.ModifiedCount : "notAcknowledged", bulkMatchRefineResult.Value.Acknowledged ? bulkMatchRefineResult.Value.ModifiedCount : "notAcknowledged", sw);

            return Result.Success();
        }
        #endregion

        #region Version
        public async Task<Result<Version>> CreateVersion(string tecdocEntityVersion, string mmiv8EntityVersion, string userName)
        {
            var userResult = await UserService.GetByName(userName);
            if (!userResult.IsSuccess)
                return Result.Failure<Version>(userResult.Error!);

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
        public async Task<Result> CreateEntityRelation(int versionNumber, List<PutEntityRelationRequest> entityRelationsRequest) //TODO Change this to allow not updating the Matchentities if not last version // This could be an endpoint
        {
            var sw = Stopwatch.StartNew();

            var versionResult = await VersionService.GetByVersion(versionNumber);
            if (!versionResult.IsSuccess)
                return versionResult;

            var entityRelations = entityRelationsRequest.ConvertAll(c => new EntityRelation(versionResult.Value.DocumentId, c.MMI_V8_Key, c.KTypNr, c.Comment, c.VersionNumber));

            var createResult = await EntityRelationService.Create([ .. entityRelations ]);
            if (!createResult.IsSuccess)
                return createResult;

            var bulkPreviousFlagUpdate = MatchEntityService.BulkCombinationUpdatePreviousMatchedFlag(entityRelations);
            var bulkPreviousFlagResult = await bulkPreviousFlagUpdate.CommitBulkWrite();
            if (!bulkPreviousFlagResult.IsSuccess)
                return bulkPreviousFlagResult;

            var bulkMatchRefineUpdate = await MatchEntityService.BulkCombinationUpdateMatchRefine(entityRelations.Select(c => c.MMI_V8_Key).Distinct());
            var bulkMatchRefineResult = await bulkMatchRefineUpdate.CommitBulkWrite();
            if (!bulkMatchRefineResult.IsSuccess)
                return bulkMatchRefineResult;

            sw.Stop();
            Log.Information("Created {entityRelationCount} EntityRelations; Updated MatchEntities: {previousFlagCount} Previous Flags; {count} MatchRefines in {time}", entityRelations.Count, bulkPreviousFlagResult.Value.Acknowledged ? bulkPreviousFlagResult.Value.ModifiedCount : "notAcknowledged", bulkMatchRefineResult.Value.Acknowledged ? bulkMatchRefineResult.Value.ModifiedCount : "notAcknowledged", sw);

            return Result.Success();
        }

        public async Task<Result> DeleteEntityRelation(List<EntityRelation> entityRelations) //TODO Change this to allow not updating the Matchentities if not last version // This could be an endpoint
        {
            var sw = Stopwatch.StartNew();

            await EntityRelationService.Delete([ .. entityRelations ]); //TODO Use a different call here or put the foreach here

            var bulkPreviousFlagUpdate = MatchEntityService.BulkCombinationUpdatePreviousMatchedFlag(entityRelations, false);
            var bulkPreviousFlagResult = await bulkPreviousFlagUpdate.CommitBulkWrite();
            if (!bulkPreviousFlagResult.IsSuccess)
                return bulkPreviousFlagResult;

            var bulkMatchRefineUpdate = await MatchEntityService.BulkCombinationUpdateMatchRefine(entityRelations.Select(c => c.MMI_V8_Key));
            var bulkMatchRefineResult = await bulkMatchRefineUpdate.CommitBulkWrite();
            if (!bulkMatchRefineResult.IsSuccess)
                return bulkMatchRefineResult;

            sw.Stop();
            Log.Information("Deleted {entityRelationCount} EntityRelations; Updated MatchEntities: {previousFlagCount} Previous Flags; {count} MatchRefines in {time}", entityRelations.Count, bulkPreviousFlagResult.Value.Acknowledged ? bulkPreviousFlagResult.Value.ModifiedCount : "notAcknowledged", bulkMatchRefineResult.Value.Acknowledged ? bulkMatchRefineResult.Value.ModifiedCount : "notAcknowledged", sw);

            return Result.Success();
        }

        public async Task<Result<Version>> GetPreviousVersionIDWithEntityRelations()
        {
            var entityRelationVersionIDs = await EntityRelationService.GetQueryable().Select(c => c.VersionID).Distinct().ToListAsync();

            var currentVersionIDResult = await VersionService.GetCurrentVersion();

            if (!currentVersionIDResult.IsSuccess)
                return currentVersionIDResult;

            Version version = await VersionService.GetQueryable()
                                                  .Where(c => entityRelationVersionIDs.Contains(c.DocumentId) && c.DocumentId != currentVersionIDResult.Value.DocumentId)
                                                  .OrderByDescending(c => c.VersionNumber)
                                                  .FirstOrDefaultAsync();

            return version is not null ? version : Error.NotFound("EntityRelation.NotFoundByPreviousVersionIDWithEntityRelations", "Cannot find Version with EntityRelations previous to current");
        }

        public async Task<Result<List<EntityRelation>>> CheckPreviousMatchedFlag(List<(int KTypNr, int MMI_V8_Key)> entityRelations)
        {
            var previousVersionResult = await GetPreviousVersionIDWithEntityRelations();

            if (!previousVersionResult.IsSuccess)
                return previousVersionResult.Error!;

            var previousEntityRelations = await EntityRelationService.GetQueryable()
                                                   .Where(c => c.VersionID == previousVersionResult.Value.DocumentId && entityRelations.Contains(new(c.KTypNr, c.MMI_V8_Key)))
                                                   .ToListAsync();

            return previousEntityRelations is { Count: > 0 } ? previousEntityRelations : Error.NoContent("EntityRelation.NotContentByPreviousRelations", "No previous relations for supplied list");
        }
        #endregion

        #region User
        public async Task<Result<DeleteResult>> DeleteUser(string userName)
        {
            var userResult = await UserService.GetByName(userName);
            if (!userResult.IsSuccess)
                return Result.Failure<DeleteResult>(userResult.Error!);

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
