using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Core.Services.Source;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Indexes;
using MMIv8_Ktype.Models.Status;
using MongoDB.Driver;
using Serilog;
using System.Threading.Tasks;

namespace MMIv8_Ktype.Core.Services.Mapping
{
    public class SourceMMIv8UpdateService(MatchMakeModelService MatchMakeModelService,
                                          MatchEntityService MatchEntityService,
                                          MappingService MappingService,
                                          SourceMMIv8Service SourceMMIv8Service,
                                          SourceTecDocPCService SourceTecDocPCService,
                                          IVersionProvider versionProvider)

        : SourceEntityUpdateService<SourceMMIv8, SourceTecDocPC>(MatchMakeModelService,
                                                                           MatchEntityService,
                                                                           MappingService,
                                                                           SourceMMIv8Service,
                                                                           SourceTecDocPCService,
                                                                           versionProvider)
    { }

    public class SourceTecDocPCUpdateService(MatchMakeModelService MatchMakeModelService,
                                             MatchEntityService MatchEntityService,
                                             MappingService MappingService,
                                             SourceMMIv8Service SourceMMIv8Service,
                                             SourceTecDocPCService SourceTecDocPCService,
                                             IVersionProvider versionProvider)

        : SourceEntityUpdateService<SourceTecDocPC, SourceMMIv8>(MatchMakeModelService,
                                                                           MatchEntityService,
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
                                                            MappingService MappingService,
                                                            SourceEntityService<TEntity> SourceEntityService,
                                                            SourceEntityService<TOther> OtherSourceEntityService,
                                                            IVersionProvider versionProvider) : ISourceEntityUpdateService<TEntity>
        where TEntity : SourceEntity
        where TOther : SourceEntity
    {
        //public CombinationPipeline<MatchEntity> CombinationPipelineUpdateEntity(SourceEntity sourceEntity) //TODO check this is still updating properly
        //{
        //    return typeof(TEntity) switch
        //    {
        //        Type t when t == typeof(MongoSourceTecDocPC) => MatchEntityService.UpdateEntity((MongoSourceTecDocPC)sourceEntity),
        //        Type t when t == typeof(MongoSourceMMIv8) => MatchEntityService.UpdateEntity((MongoSourceMMIv8)sourceEntity),
        //        _ => throw new NotImplementedException()
        //    };
        //}

        public FilterDefinition<MatchEntity> SourceEntityFilter(SourceEntity sourceEntity) //TODO check this is still updating properly
        {
            var filterBuilder = Builders<MatchEntity>.Filter;
            return typeof(TEntity) switch
            {
                Type t when t == typeof(SourceTecDocPC) => filterBuilder.Eq(e => e.TecDocEntity.DocumentId, sourceEntity.DocumentId),
                Type t when t == typeof(SourceMMIv8) => filterBuilder.Eq(e => e.MMIv8Entity.DocumentId, sourceEntity.DocumentId),
                _ => throw new NotImplementedException()
            };
        }

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

            var matchEntityFilterSourceEntity = SourceEntityFilter(sourceEntity);
            List <MatchMakeModel> newMatchesToCreate = new();

            var bulkCombinationUpdate = MatchEntityService.CreateBulkCombinationUpdate();

            if (currentEntityResult.IsSuccess) //Entity does exist -> update entity
            {
                if (currentEntityResult.Value.EntityHash == sourceEntity.EntityHash)
                    return Error.Validation($"{typeof(TEntity).Name}.UpdateValidation", "No changes detected");

                sourceEntity = await SourceEntityService.UpdateDifferences(currentEntityResult.Value, sourceEntity);

                if (currentEntityResult.Value.SourceEntityModelHash == sourceEntity.SourceEntityModelHash)
                {
                    var matchEntityUpdate = MatchEntityService.Update(matchEntityFilterSourceEntity).AppendUpdate(c => c.UpdateEntity(sourceEntity)) //TODO check this is still updating properly
                                                                                                    .AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Check, $"Updated {typeof(TEntity).Name} Entity")));
                    var matchEntityResult = await matchEntityUpdate.UpdateDocuments();
                }
                else
                {
                    var currentMakeModelMatches = await MatchMakeModelService.GetByModelId(currentEntityResult.Value.SourceIndex, currentEntityResult.Value.SourceEntityModelHash, true);

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

                            bulkCombinationUpdate.Combine(await MatchEntityService.BulkCombinationUpdateMatchRefine(matchMakeModelFilter));
                        }
                    }

                    var bulkCombinationResult = await bulkCombinationUpdate.CommitBulkWrite();

                    // Delete MatchEntity records with source entity
                    var orphanedCount = await MatchEntityService.CountByFilter(matchEntityFilterSourceEntity);

                    if (orphanedCount > 0)
                        Log.Error("{count} MatchEntities left oprhaned for entity {@sourceEntity}", orphanedCount, sourceEntity);

                    newMatchesToCreate = await MatchMakeModelService.GetByModelId(sourceEntity.SourceIndex, sourceEntity.SourceEntityModelHash, true);
                }
            }
            else if (currentEntityResult.Error!.Type == ErrorType.NotFound) //Entity doesnt exist -> create entity
            {
                var createEntityResult = await SourceEntityService.Create(sourceEntity);

                if (!createEntityResult.IsSuccess)
                    return createEntityResult;

                newMatchesToCreate = await MatchMakeModelService.GetByModelId(sourceEntity.SourceIndex, sourceEntity.SourceEntityModelHash, true);
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
