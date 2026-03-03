using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Core.Services.Source;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Indexes;
using MongoDB.Driver;
using Serilog;
using System.Diagnostics;

namespace MMIv8_Ktype.Core.Services.Mapping
{
    public class SourceMMIv8MatchRefineService(SourceMMIv8Service SourceEntityService, MatchEntityService MatchEntityService) : SourceEntityMatchRefineService<SourceMMIv8>(SourceEntityService, MatchEntityService)
    {
    }

    public class SourceTecDocPCMatchRefineService(SourceTecDocPCService SourceEntityService, MatchEntityService MatchEntityService) : SourceEntityMatchRefineService<SourceTecDocPC>(SourceEntityService, MatchEntityService)
    {
    }

    public abstract class SourceEntityMatchRefineService<T>(SourceEntityService<T> SourceEntityService, MatchEntityService MatchEntityService)
        where T : SourceEntity
    {
        private async Task<ICombinationPipeline[]> PrepareEntityMatchRefine(int externalId)
        {
            var sw = Stopwatch.StartNew();
            var filter = Builders<T>.Filter.Eq(e => e.ExternalId, externalId);
            var entityMatchFilter = SourceEntityService.SourceEntityFilter(externalId);
            var matchEntities = await MatchEntityService.GetFindFluent(entityMatchFilter).ToListAsync();
            var matchRefine = new MatchRefine(matchEntities);

            Log.Debug("Prepped MatchRefine for {SourceIndex} - {externalId} ({count}) in {sw}", SourceEntityService.SourceIndex, externalId, matchEntities.Count, sw);

            return [
                MatchEntityService.Update(entityMatchFilter).AppendUpdate(c => c.UpdateEntityMatchRefine(SourceEntityService.SourceIndex, matchRefine)),
                SourceEntityService.Update(filter).AppendUpdate(c => c.UpdateMatchRefine(matchRefine))
                ];
        }

        private async Task<ICombinationPipeline[]> PrepareEntityMatchRefine(T sourceEntity)
        {
            var sw = Stopwatch.StartNew();
            var filter = SourceEntityService.SourceEntityFilter(sourceEntity);
            var matchEntities = await MatchEntityService.GetFindFluent(filter).ToListAsync();
            var matchRefine = new MatchRefine(matchEntities);

            Log.Debug("Prepped MatchRefine with SourceEntity for {SourceIndex} - {externalId} ({count}) in {sw}", SourceEntityService.SourceIndex, sourceEntity.ExternalId, matchEntities.Count, sw);

            if (matchRefine == sourceEntity.MatchRefine)
                return [];

            return [
                MatchEntityService.Update(filter).AppendUpdate(c => c.UpdateEntityMatchRefine(sourceEntity.SourceIndex, matchRefine)),
                SourceEntityService.Update(sourceEntity).AppendUpdate(c => c.UpdateMatchRefine(matchRefine))
                ];
        }

        private async IAsyncEnumerable<ICombinationPipeline> EnumerateCombinationUpdateMatchRefines(FilterDefinition<MatchEntity> filter)
        {
            using var entities = await MatchEntityService.GetDistinctCursor<int>($"{SourceEntityService.MatchEntitySourceEntityFieldName}.ExternalId", filter);
            {
                while (await entities.MoveNextAsync())
                {
                    foreach (var sourceEntity in entities.Current)
                    {
                        foreach (var combinationUpdate in await PrepareEntityMatchRefine(sourceEntity))
                            yield return combinationUpdate;
                    }
                }
            }
        }

        private async Task<IEnumerable<ICombinationPipeline>> CombinationUpdateMatchRefinesList(FilterDefinition<MatchEntity> filter)
        {
            List<ICombinationPipeline> combinationUpdates = new();
            await foreach (var combinationUpdate in EnumerateCombinationUpdateMatchRefines(filter))
            {
                combinationUpdates.Add(combinationUpdate);
            }
            return combinationUpdates;
        }


        public async Task<BulkCombinationUpdate> BulkCombinationUpdateMatchRefine(IEnumerable<int> externalIds) //TODO change or remove this
        {
            var tasks = externalIds.Select(c => PrepareEntityMatchRefine(c)).ToArray();
            var combinationUpdates = await Task.WhenAll(tasks);

            return SourceEntityService.CreateBulkCombinationUpdate().AddCombinationUpdate(combinationUpdates.SelectMany(x => x));
        }

        public async Task<BulkCombinationUpdate> BulkCombinationUpdateMatchRefine(FilterDefinition<MatchEntity> filter)
        {
            return SourceEntityService.CreateBulkCombinationUpdate().AddCombinationUpdate(await CombinationUpdateMatchRefinesList(filter));
        }

    }
}
