using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Indexes;
using MMIv8_Ktype.Models.Status;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace MMIv8_Ktype.Core.Services.Match
{
    public class MatchMakeModelService(MongoDBContext MMIv8_Ktype,
                                       MongoBaseContext BaseContext,
                                       IVersionProvider versionProvider) : IMongoCollectionService<MatchMakeModel>
    {
        public IMongoCollection<MatchMakeModel> Collection => MMIv8_Ktype.Collections.MatchMakeModel;

        public async Task<IAsyncCursor<MatchMakeModel>> GetAll(FilterDefinition<MatchMakeModel>? filter = null, int? batchSize = null)
        {
            return await BaseContext.GetCursor(Collection, filter: filter, batchSize: batchSize);
        }

        public async Task<IAsyncCursor<MatchMakeModel>> GetAllMatches(FilterDefinition<MatchMakeModel>? filter = null, int? batchSize = 1000)
        {
            var filterBuilder = Builders<MatchMakeModel>.Filter;
            filter ??= filterBuilder.Empty;
            filter &= filterBuilder.Exists(m => m.TecDocModel.SourceEntityModelHash)
                    & filterBuilder.Exists(m => m.MMIv8Model.SourceEntityModelHash)
                    & filterBuilder.Ne(x => x.Status.Current.Status, Status.Deprecated);

            return await BaseContext.GetCursor(Collection, filter, batchSize: batchSize);
        }

        public async Task<MatchMakeModel?> GetById(ObjectId MatchID)
        {
            var filter = Builders<MatchMakeModel>.Filter.Eq(e => e.MatchID, MatchID);
            return await BaseContext.GetSingleDocument(Collection, filter);
        }

        public async Task<IAsyncCursor<MatchMakeModel>> GetByModelId(SourceIndex sourceIndex, string SourceEntityModelHash)
        {
            var builder = Builders<MatchMakeModel>.Filter;
            var filter = builder.Empty;

            if (sourceIndex == SourceIndex.TecDocPC)
                filter = builder.Eq(e => e.TecDocModel.SourceEntityModelHash, SourceEntityModelHash);

            if (sourceIndex == SourceIndex.MMIv8)
                filter = builder.Eq(e => e.MMIv8Model.SourceEntityModelHash, SourceEntityModelHash);

            return await BaseContext.GetCursor(Collection, filter);
        }

        public async Task<MatchMakeModel?> GetByModelIds(ImportMatchMakeModel model)
        {
            var builder = Builders<MatchMakeModel>.Filter;
            var filter = builder.Eq(e => e.TecDocModel.SourceEntityModelHash, model.TD_SourceEntityModelHash)
                       & builder.Eq(e => e.MMIv8Model.SourceEntityModelHash, model.MMI_SourceEntityModelHash);

            return await BaseContext.GetSingleDocument(Collection, filter);
        }

        public async Task<bool> CheckValid(MongoSourceEntityModel tecdoc, MongoSourceEntityModel mmiv8)
        {
            var builder = Builders<MatchMakeModel>.Filter;
            var filter = builder.Eq(m => m.TecDocModel.SourceEntityModelHash, tecdoc.SourceEntityModelHash) 
                       & builder.Eq(m => m.MMIv8Model.SourceEntityModelHash, mmiv8.SourceEntityModelHash)
                       & builder.Ne(x => x.Status.Current.Status, Status.Deprecated);

            bool match = await BaseContext.GetSingleDocument(Collection, filter) is not null;

            return match;
        }

        public async Task Create(MatchMakeModel model)
        {
            try
            {
                await BaseContext.Create(Collection, model);

                await UpdateRelatedMatches(model, $"Added match {model.MatchID.ToString()}");
            }
            catch (MongoWriteException mwe)
            {
                Log.Error(mwe, "Make Model Match creation error");
            }
        }

        private async Task<UpdateResult?> UpdateRelatedMatches(MatchMakeModel model, string? detail = null)
        {
            var filterBuilder = Builders<MatchMakeModel>.Filter;

            var tecdocMatches = await GetByModelId(SourceIndex.TecDocPC, model.TecDocModel.SourceEntityModelHash);
            var mmiMatches = await GetByModelId(SourceIndex.MMIv8, model.MMIv8Model.SourceEntityModelHash);

            var updateFilter = filterBuilder.Exists(m => m.TecDocModel.SourceEntityModelHash) & filterBuilder.Exists(m => m.MMIv8Model.SourceEntityModelHash);
            var tecdocFilter = filterBuilder.In(m => m.TecDocModel.SourceEntityModelHash, mmiMatches.ToList().Select(m => m.TecDocModel.SourceEntityModelHash));
            var mmiFilter = filterBuilder.In(m => m.MMIv8Model.SourceEntityModelHash, tecdocMatches.ToList().Select(m => m.MMIv8Model.SourceEntityModelHash));

            updateFilter &= tecdocFilter | mmiFilter;

            var matchMakeModelUpdate = new CombinationPipeline<MatchMakeModel>(Collection, updateFilter).AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status.Check, detail)));
            return await matchMakeModelUpdate.UpdateDocuments();
        }

        public async Task Delete(MatchMakeModel model)
        {
            var builder = Builders<MatchMakeModel>.Filter;
            var filter = builder.Eq(c => c.MatchID, model.MatchID);

            try
            {
                await BaseContext.Delete(Collection, filter);

                await UpdateRelatedMatches(model, $"Deleted match {model.MatchID.ToString()}");
            }
            catch (MongoWriteException mwe)
            {
                Log.Error(mwe, "Make Model Match deletion error");
            }
        }

        public async Task DeleteAll()
        {
            await BaseContext.Delete(Collection, Builders<MatchMakeModel>.Filter.Empty);
        }

        public async Task<UpdateResult?> UpdateMatchStatus(Status status, FilterDefinition<MatchMakeModel>? filter = null)
        {
            filter ??= Builders<MatchMakeModel>.Filter.Empty;

            var matchMakeModelUpdate = new CombinationPipeline<MatchMakeModel>(Collection, filter).AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(status)));
            return await matchMakeModelUpdate.UpdateDocuments();
        }
    }
}
