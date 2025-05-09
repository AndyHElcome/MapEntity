using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Core.Services.Source;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Indexes;
using MMIv8_Ktype.Models.Status;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace MMIv8_Ktype.Core.Services.Mapping
{
    public class SourceMMIv8UpdateService(MatchMakeModelService MatchMakeModelService,
                                          MatchEntityService MatchEntityService,
                                          MappingService MappingService,
                                          SourceMMIv8Service SourceMMIv8Service,
                                          SourceTecDocPCService SourceTecDocPCService,
                                          IVersionProvider versionProvider)

        : SourceEntityUpdateService<MongoSourceMMIv8, MongoSourceTecDocPC>(MatchMakeModelService,
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

        : SourceEntityUpdateService<MongoSourceTecDocPC, MongoSourceMMIv8>(MatchMakeModelService,
                                                                           MatchEntityService,
                                                                           MappingService,
                                                                           SourceTecDocPCService,
                                                                           SourceMMIv8Service,
                                                                           versionProvider)
    { }

    public interface ISourceEntityUpdateService<TEntity>
        where TEntity : SourceEntity
    {
        Task UpdateEntity(TEntity sourceEntity, string? detail = null);
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
        public CombinationPipeline<MatchEntity> CombinationPipelineUpdateEntity(SourceEntity sourceEntity) //TODO check this is still updating properly
        {
            return typeof(TEntity) switch
            {
                Type t when t == typeof(MongoSourceTecDocPC) => MatchEntityService.UpdateEntity((MongoSourceTecDocPC)sourceEntity),
                Type t when t == typeof(MongoSourceMMIv8) => MatchEntityService.UpdateEntity((MongoSourceMMIv8)sourceEntity),
                _ => throw new NotImplementedException()
            };
        }

        public FilterDefinition<MatchEntity> SourceMatchEntityFilter(SourceEntity sourceEntity) //TODO check this is still updating properly
        {
            var filterBuilder = Builders<MatchEntity>.Filter;
            return typeof(TEntity) switch
            {
                Type t when t == typeof(MongoSourceTecDocPC) => filterBuilder.Eq(e => e.TecDocEntity.DocumentId, sourceEntity.DocumentId),
                Type t when t == typeof(MongoSourceMMIv8) => filterBuilder.Eq(e => e.MMIv8Entity.DocumentId, sourceEntity.DocumentId),
                _ => throw new NotImplementedException()
            };
        }

        public async Task<List<TIdx>> GetEntities<TIdx>(string sourceEntityModelHash) //TODO check this is still updating properly
        {
            IAsyncCursor<TIdx> entities = typeof(TEntity) switch
            {
                Type t when t == typeof(TIdx) => (IAsyncCursor<TIdx>)await SourceEntityService.GetByModelId(sourceEntityModelHash),
                Type t when t != typeof(TIdx) => (IAsyncCursor<TIdx>)await OtherSourceEntityService.GetByModelId(sourceEntityModelHash),
                _ => throw new NotImplementedException()
            };

            return await entities.ToListAsync();
        }

        public async Task UpdateEntity(TEntity sourceEntity, string? detail = null) // TODO get all entities and filter where Hash is different to rule out nonchanges
        {
            var currentEntity = await SourceEntityService.GetByExternalId(sourceEntity.ExternalId);

            if (currentEntity is not null && currentEntity.EntityHash == sourceEntity.EntityHash)
                return;

            if (currentEntity is null)
            {
                await SourceEntityService.CreateAndValidate(sourceEntity);
            }
            else
            {
                string? differences = null;
                var sourceEntityUpdate = SourceEntityService.UpdateSourceEntity(currentEntity)
                                                            .AppendUpdate(c => c.UpdateDifferences(currentEntity, sourceEntity, out differences))
                                                            .AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Updated, $"Entity Updated: '{differences}'")));
                sourceEntity = await sourceEntityUpdate.FindAndUpdateDocument();
            }

            var filter = SourceMatchEntityFilter(sourceEntity);

            if (currentEntity is null || currentEntity.SourceEntityModelHash != sourceEntity.SourceEntityModelHash)
            {
                await MatchEntityService.DeleteByFilter(filter); //TODO Create previous match collection and then keep these updated

                using var makeModelMatches = await MatchMakeModelService.GetByModelId(sourceEntity.SourceIndex, sourceEntity.SourceEntityModelHash);
                while (await makeModelMatches.MoveNextAsync())
                {
                    foreach (var makeModelMatch in makeModelMatches.Current)
                    {
                        var tecdocEntities = await GetEntities<MongoSourceTecDocPC>(makeModelMatch.TecDocModel.DocumentId);
                        var mmiEntities = await GetEntities<MongoSourceMMIv8>(makeModelMatch.MMIv8Model.DocumentId);

                        await foreach (var newMatch in MappingService.GenerateEntityMatch(tecdocEntities, mmiEntities, makeModelMatch.DocumentId))
                        {
                            await MappingService.BulkCreateEntityMatch(newMatch.ToList());
                        }
                    }
                }

                var matchEntityUpdate = new CombinationPipeline<MatchEntity>(MatchEntityService.Collection, filter).AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Check, $"Updated {typeof(TEntity)} Entity")));
                var matchEntityResult = await matchEntityUpdate.UpdateDocuments();
                await MappingService.RecalculateMatchBase(matchEntityUpdate.Filter);
            }
            else
            {
                var matchEntityUpdate = CombinationPipelineUpdateEntity(sourceEntity).AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Check, $"Updated {typeof(TEntity)} Entity")));
                var matchEntityResult = await matchEntityUpdate.UpdateDocuments();
                await MappingService.RecalculateMatchBase(matchEntityUpdate.Filter);
            }
        }

    }
}
