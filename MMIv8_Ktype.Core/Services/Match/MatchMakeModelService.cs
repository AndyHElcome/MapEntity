using MMIv8_Ktype.Api.Requests;
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
    public class MatchMakeModelService(MongoDBContext MMIv8_Ktype, IVersionProvider versionProvider) : BaseServiceWithVersion<MatchMakeModel, ObjectId>(MMIv8_Ktype.Collections.MatchMakeModel, versionProvider)
    {
        public async Task<IAsyncCursor<MatchMakeModel>> GetAllMatches(FilterDefinition<MatchMakeModel>? filter = null, int? batchSize = 1000)
        {
            var filterBuilder = Builders<MatchMakeModel>.Filter;
            filter ??= filterBuilder.Empty;
            filter &= filterBuilder.Exists(m => m.TecDocModel.DocumentId)
                    & filterBuilder.Exists(m => m.MMIv8Model.DocumentId)
                    & filterBuilder.Ne(x => x.Status.Current.Status, Status.Deprecated);

            return await base.GetCursor(filter, batchSize: batchSize);
        }

        public async Task<IAsyncCursor<MatchMakeModel>> GetByModelId(SourceIndex sourceIndex, string SourceEntityModelHash)
        {
            var builder = Builders<MatchMakeModel>.Filter;
            var filter = builder.Empty;

            if (sourceIndex == SourceIndex.TecDocPC)
                filter = builder.Eq(e => e.TecDocModel.DocumentId, SourceEntityModelHash);

            if (sourceIndex == SourceIndex.MMIv8)
                filter = builder.Eq(e => e.MMIv8Model.DocumentId, SourceEntityModelHash);

            return await base.GetCursor(filter);
        }

        public async Task<MatchMakeModel?> GetByModelIds(MatchMakeModelRequest request)
        {
            var builder = Builders<MatchMakeModel>.Filter;
            var filter = builder.Empty;

            if (request.TD_SourceEntityModelHash != "")
            {
                filter &= builder.Eq(e => e.TecDocModel.DocumentId, request.TD_SourceEntityModelHash);
            }
            else
            {
                filter &= builder.Exists(e => e.TecDocModel.DocumentId, false);
            }

            if (request.MMI_SourceEntityModelHash != "")
            {
                filter &= builder.Eq(e => e.MMIv8Model.DocumentId, request.MMI_SourceEntityModelHash);
            }
            else
            {
                filter &= builder.Exists(e => e.MMIv8Model.DocumentId, false);
            }

            return await base.GetSingleDocument(filter);
        }

        public async Task<bool> CheckValid(MongoSourceEntityModel tecdoc, MongoSourceEntityModel mmiv8)
        {
            var builder = Builders<MatchMakeModel>.Filter;
            var filter = builder.Eq(m => m.TecDocModel.DocumentId, tecdoc.DocumentId) 
                       & builder.Eq(m => m.MMIv8Model.DocumentId, mmiv8.DocumentId)
                       & builder.Ne(x => x.Status.Current.Status, Status.Deprecated);

            bool match = await base.GetSingleDocument(filter) is not null;

            return match;
        }

        public new async Task Create(MatchMakeModel model)
        {
            try
            {
                await base.Create(model);

                await UpdateRelatedMatches(model, $"Added match {model.DocumentId.ToString()}");
            }
            catch (MongoWriteException mwe)
            {
                Log.Error(mwe, "Make Model Match creation error");
            }
        }

        private async Task<UpdateResult?> UpdateRelatedMatches(MatchMakeModel model, string? detail = null)
        {
            var filterBuilder = Builders<MatchMakeModel>.Filter;

            var tecdocMatches = await GetByModelId(SourceIndex.TecDocPC, model.TecDocModel.DocumentId);
            var mmiMatches = await GetByModelId(SourceIndex.MMIv8, model.MMIv8Model.DocumentId);

            var updateFilter = filterBuilder.Exists(m => m.TecDocModel.DocumentId) & filterBuilder.Exists(m => m.MMIv8Model.DocumentId);
            var tecdocFilter = filterBuilder.In(m => m.TecDocModel.DocumentId, mmiMatches.ToList().Select(m => m.TecDocModel.DocumentId));
            var mmiFilter = filterBuilder.In(m => m.MMIv8Model.DocumentId, tecdocMatches.ToList().Select(m => m.MMIv8Model.DocumentId));

            updateFilter &= tecdocFilter | mmiFilter;

            return await base.UpdateStatus(updateFilter, Status.Check, detail);
        }
    }
}
