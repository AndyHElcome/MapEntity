using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Core.Services.Source;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Indexes;
using MMIv8_Ktype.Models.Status;
using MongoDB.Driver;

namespace MMIv8_Ktype.Core.Services.Mapping
{
    public class SourceMMIv8UpdateService(MatchMakeModelService MatchMakeModelService,
                                          MatchEntityService MatchEntityService,
                                          MappingService MappingService,
                                          SourceMMIv8Service SourceMMIv8Service,
                                          SourceTecDocPCService SourceTecDocPCService,
                                          SourceMMIv8Service SourceEntityService,
                                          IVersionProvider versionProvider)

        : SourceEntityUpdateService<MongoSourceMMIv8>(MatchMakeModelService,
                                                      MatchEntityService,
                                                      MappingService,
                                                      SourceMMIv8Service,
                                                      SourceTecDocPCService,
                                                      SourceEntityService,
                                                      versionProvider)
    { }

    public class SourceTecDocPCUpdateService(MatchMakeModelService MatchMakeModelService,
                                             MatchEntityService MatchEntityService,
                                             MappingService MappingService,
                                             SourceMMIv8Service SourceMMIv8Service,
                                             SourceTecDocPCService SourceTecDocPCService,
                                             SourceTecDocPCService SourceEntityService,
                                             IVersionProvider versionProvider)

        : SourceEntityUpdateService<MongoSourceTecDocPC>(MatchMakeModelService,
                                                         MatchEntityService,
                                                         MappingService,
                                                         SourceMMIv8Service,
                                                         SourceTecDocPCService,
                                                         SourceEntityService,
                                                         versionProvider)
    { }

    public class SourceEntityUpdateService<T>(MatchMakeModelService MatchMakeModelService,
                                              MatchEntityService MatchEntityService,
                                              MappingService MappingService,
                                              SourceMMIv8Service SourceMMIv8Service,
                                              SourceTecDocPCService SourceTecDocPCService,
                                              SourceEntityService<T> SourceEntityService,
                                              IVersionProvider versionProvider)
        where T : SourceEntity
    {
        public async Task UpdateEntity(T sourceEntity, string? detail = null) // TODO get all entities and filter where Hash is different to rule out nonchanges

        {
            var currentEntity = await SourceEntityService.GetByExternalId(sourceEntity.ExternalId);

            if (currentEntity is not null && currentEntity.EntityHash == sourceEntity.EntityHash)
                return;

            if (currentEntity is null)
            {
                await SourceEntityService.Create(sourceEntity);
            }
            else
            {
                string? differences = null;
                var sourceEntityUpdate = SourceEntityService.UpdateSourceEntity(currentEntity)
                                                            .AppendUpdate(c => c.UpdateDifferences(currentEntity, sourceEntity, out differences))
                                                            .AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Updated, $"Entity Updated: '{differences}'")));
                sourceEntity = await sourceEntityUpdate.FindAndUpdateDocument();
            }

            var filterBuilder = Builders<MatchEntity>.Filter;
            var filter = filterBuilder.Eq(e => e.MMIv8Entity.SourceEntityID, sourceEntity.SourceEntityID);

            if (currentEntity is null || currentEntity.SourceEntityModelHash != sourceEntity.SourceEntityModelHash)
            {
                await MatchEntityService.Delete(filter); //TODO Create previous match collection and then keep these updated

                using var makeModelMatches = await MatchMakeModelService.GetByModelId(sourceEntity.SourceIndex, sourceEntity.SourceEntityModelHash);
                while (await makeModelMatches.MoveNextAsync())
                {
                    foreach (var makeModelMatch in makeModelMatches.Current)
                    {
                        var tecdocEntities = await SourceTecDocPCService.GetByModelId(makeModelMatch.TecDocModel.SourceEntityModelHash);
                        var mmiEntities = await SourceMMIv8Service.GetByModelId(makeModelMatch.MMIv8Model.SourceEntityModelHash);

                        await foreach (var newMatch in MappingService.GenerateEntityMatch(await tecdocEntities.ToListAsync(), await mmiEntities.ToListAsync(), makeModelMatch.MatchID))
                        {
                            await MatchEntityService.BulkCreateEntityMatch(newMatch.ToList());
                        }
                    }
                }

                var matchEntityUpdate = new CombinationPipeline<MatchEntity>(MatchEntityService.Collection, filter).AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Check, $"Updated {typeof(T)} Entity")));
                var matchEntityResult = await matchEntityUpdate.UpdateDocuments();
                await MappingService.RecalculateMatchBase(matchEntityUpdate.Filter);
            }
            else
            {
                var matchEntityUpdate = MatchEntityService.UpdateEntity(sourceEntity).AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Check, $"Updated {typeof(T)} Entity")));
                var matchEntityResult = await matchEntityUpdate.UpdateDocuments();
                await MappingService.RecalculateMatchBase(matchEntityUpdate.Filter);
            }
        }
    }
}
