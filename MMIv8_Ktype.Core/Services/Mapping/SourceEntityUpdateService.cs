using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Core.Services.Source;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Indexes;
using MMIv8_Ktype.Models.Status;
using MongoDB.Driver;
using Serilog;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MMIv8_Ktype.Core.Services.Mapping
{

    public class SourceMMIv8UpdateService(MatchMakeModelService MatchMakeModelService,
                                          MatchEntityService MatchEntityService,
                                          SourceMMIv8MatchRefineService SourceEntityMatchRefineService,
                                          MappingService MappingService,
                                          SourceMMIv8Service SourceMMIv8Service,
                                          SourceTecDocPCService SourceTecDocPCService,
                                          IVersionProvider versionProvider)

        : SourceEntityUpdateService<SourceMMIv8, SourceTecDocPC>(MatchMakeModelService,
                                                                           MatchEntityService,
                                                                           SourceEntityMatchRefineService,
                                                                           MappingService,
                                                                           SourceMMIv8Service,
                                                                           SourceTecDocPCService,
                                                                           versionProvider)
    { }

    public class SourceTecDocPCUpdateService(MatchMakeModelService MatchMakeModelService,
                                             MatchEntityService MatchEntityService,
                                             SourceTecDocPCMatchRefineService SourceEntityMatchRefineService,
                                             MappingService MappingService,
                                             SourceMMIv8Service SourceMMIv8Service,
                                             SourceTecDocPCService SourceTecDocPCService,
                                             IVersionProvider versionProvider)

        : SourceEntityUpdateService<SourceTecDocPC, SourceMMIv8>(MatchMakeModelService,
                                                                           MatchEntityService,
                                                                           SourceEntityMatchRefineService,
                                                                           MappingService,
                                                                           SourceTecDocPCService,
                                                                           SourceMMIv8Service,
                                                                           versionProvider)
    { }

    public interface ISourceEntityUpdateService<TEntity>
        where TEntity : SourceEntity
    {
        Task<Result> UpdateEntity(TEntity sourceEntity, string? detail = null);
    }

    public class SourceEntityUpdateService<TEntity, TOther>(MatchMakeModelService MatchMakeModelService,
                                                            MatchEntityService MatchEntityService,
                                                            SourceEntityMatchRefineService<TEntity> SourceEntityMatchRefineService,
                                                            MappingService MappingService,
                                                            SourceEntityService<TEntity> SourceEntityService,
                                                            SourceEntityService<TOther> OtherSourceEntityService,
                                                            IVersionProvider versionProvider) : ISourceEntityUpdateService<TEntity>
        where TEntity : SourceEntity
        where TOther : SourceEntity
    {
        /// <summary>
        /// Checks if Provided Type is the Source Entity Type, if so returns the source Entity, if not returns all of the other index entities.
        /// </summary>
        /// <typeparam name="TIdx"></typeparam>
        /// <param name="sourceEntity"></param>
        /// <param name="sourceEntityModelHash"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<List<TIdx>> GetEntities<TIdx>(SourceEntity sourceEntity, string sourceEntityModelHash) //TODO check this is still updating properly
            where TIdx : SourceEntity
        {
            if (typeof(TEntity) == typeof(TIdx)) // Source Index -> just return the source Entity 
            {
                return [ (TIdx)sourceEntity ];
            }
            else //Other index -> get other Entities under each MakeModelMatch
            {
                var otherIndexEntities = await OtherSourceEntityService.GetByModelId(sourceEntityModelHash);

                if (otherIndexEntities.IsSuccess)
                    return [ .. otherIndexEntities.Value.Cast<TIdx>() ];
            }

            return new();
        }

        public async Task<Result> UpdateEntity(TEntity sourceEntity, string? detail = null) // TODO get all entities and filter where Hash is different to rule out nonchanges
        {
            var currentEntityResult = await SourceEntityService.GetByExternalId(sourceEntity.ExternalId);

            var matchEntityFilterSourceEntity = SourceEntityService.SourceEntityFilter(sourceEntity);
            List <MatchMakeModel> newMatchesToCreate = new();

            var bulkCombinationUpdate = MatchEntityService.CreateBulkCombinationUpdate();

            if (currentEntityResult.IsSuccess) //Entity does exist -> update entity
            {
                if (currentEntityResult.Value.EntityHash == sourceEntity.EntityHash)
                    return Error.Validation($"{typeof(TEntity).Name}.UpdateValidation", "No changes detected");

                var updateEntityResult = await SourceEntityService.UpdateDifferences(currentEntityResult.Value, sourceEntity);
                if (!updateEntityResult.IsSuccess)
                    return updateEntityResult;

                sourceEntity = updateEntityResult.Value;

                if (currentEntityResult.Value.SourceEntityModelHash == sourceEntity.SourceEntityModelHash)
                {
                    var matchEntityUpdate = MatchEntityService.Update(matchEntityFilterSourceEntity).AppendUpdate(c => c.UpdateEntity(sourceEntity)) //TODO check this is still updating properly
                                                                                                    .AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Check, $"Updated {typeof(TEntity).Name} Entity")));
                    var matchEntityResult = await matchEntityUpdate.UpdateDocuments();

                    //Added this as it should continue when there are duplicates
                    var sourceMakeModelMatchesResult = await MatchMakeModelService.GetByModelId(sourceEntity.SourceIndex, sourceEntity.SourceEntityModelHash, true);
                    newMatchesToCreate = sourceMakeModelMatchesResult.IsSuccess ? sourceMakeModelMatchesResult.Value : new();
                }
                else
                {
                    var currentMakeModelMatchesResult = await MatchMakeModelService.GetByModelId(currentEntityResult.Value.SourceIndex, currentEntityResult.Value.SourceEntityModelHash, true);
                    var currentMakeModelMatches = currentMakeModelMatchesResult.IsSuccess ? currentMakeModelMatchesResult.Value : new();

                    foreach (var currentMakeModelMatch in currentMakeModelMatches)
                    {
                        var matchMakeModelFilter = Builders<MatchEntity>.Filter.Eq(c => c.MatchMakeModelMatchID, currentMakeModelMatch.DocumentId);

                        var deleteResult = await MatchEntityService.DeleteByFilter(matchMakeModelFilter & matchEntityFilterSourceEntity);

                        if (!deleteResult.IsSuccess)
                            return deleteResult;

                        if (deleteResult.Value.IsAcknowledged && deleteResult.Value.DeletedCount > 0)
                        {
                            var matchEntityUpdate = MatchEntityService.Update(matchMakeModelFilter).AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Check, $"Deleted {typeof(TEntity).Name} Entity {sourceEntity.DocumentId} as it changes it's MakeModel")));
                            var matchEntityResult = await matchEntityUpdate.UpdateDocuments();

                            bulkCombinationUpdate.Combine(await SourceEntityMatchRefineService.BulkCombinationUpdateMatchRefine(matchMakeModelFilter));
                        }
                    }

                    var bulkCombinationResult = await bulkCombinationUpdate.CommitBulkWrite();

                    // Delete MatchEntity records with source entity
                    var orphanedCount = await MatchEntityService.CountByFilter(matchEntityFilterSourceEntity);

                    if (orphanedCount > 0)
                        Log.Error("{count} MatchEntities left oprhaned for entity {@sourceEntity}", orphanedCount, sourceEntity);

                    var sourceMakeModelMatchesResult = await MatchMakeModelService.GetByModelId(sourceEntity.SourceIndex, sourceEntity.SourceEntityModelHash, true);
                    newMatchesToCreate = sourceMakeModelMatchesResult.IsSuccess ? sourceMakeModelMatchesResult.Value : new();
                }
            }
            else if (currentEntityResult.Error!.Type == ErrorType.NotFound) //Entity doesnt exist -> create entity
            {
                var createEntityResult = await SourceEntityService.Create(sourceEntity);

                if (!createEntityResult.IsSuccess)
                    return createEntityResult;

                var sourceMakeModelMatchesResult = await MatchMakeModelService.GetByModelId(sourceEntity.SourceIndex, sourceEntity.SourceEntityModelHash, true);
                newMatchesToCreate = sourceMakeModelMatchesResult.IsSuccess ? sourceMakeModelMatchesResult.Value : new();
            }
            else //Entity result threw something unexpected
            {
                throw new NotImplementedException();
            }

            foreach (var makeModelMatch in newMatchesToCreate)
            {
                var tecdocEntities = await GetEntities<SourceTecDocPC>(sourceEntity, makeModelMatch.TecDocModel.DocumentId);
                var mmiEntities = await GetEntities<SourceMMIv8>(sourceEntity, makeModelMatch.MMIv8Model.DocumentId);

                await foreach (var newMatch in MappingService.GenerateEntityMatch(tecdocEntities, mmiEntities, makeModelMatch.DocumentId))
                {
                    await MappingService.BulkCreateEntityMatch(newMatch.ToList());
                }

                var matchMakeModelFilter = Builders<MatchEntity>.Filter.Eq(c => c.MatchMakeModelMatchID, makeModelMatch.DocumentId);

                var matchEntityResult = MatchEntityService.UpdateStatus(matchMakeModelFilter, Status.Check, $"Updated {typeof(TEntity).Name} Entity");
            }

            await MappingService.RecalculateMatchBase(matchEntityFilterSourceEntity);

            Log.Information("Updated Entity");
            return Result.Success();
        }
    }
}
