using Microsoft.AspNetCore.Mvc;
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
        [Obsolete]
        public async Task<IAsyncCursor<MatchMakeModel>> GetAllMatches(FilterDefinition<MatchMakeModel>? filter = null, int? batchSize = 1000, bool validOnly = true)
        {
            var filterBuilder = Builders<MatchMakeModel>.Filter;
            filter ??= filterBuilder.Empty;

            if (validOnly)
                filter &= filterBuilder.Exists(m => m.TecDocModel.DocumentId)
                    & filterBuilder.Exists(m => m.MMIv8Model.DocumentId)
                    & filterBuilder.Ne(x => x.Status.Current.Status, Status.Deprecated);

            return await base.GetFindFluent(filter, batchSize: batchSize).ToCursorAsync();
        }

        public async Task<Result<List<MatchMakeModel>>> GetByModelId(SourceIndex sourceIndex, string SourceEntityModelHash, bool validOnly = false)
        {
            try
            {
                var builder = Builders<MatchMakeModel>.Filter;
                var filter = builder.Empty;

                if (sourceIndex == SourceIndex.TecDocPC)
                    filter = builder.Eq(e => e.TecDocModel.DocumentId, SourceEntityModelHash);

                if (sourceIndex == SourceIndex.MMIv8)
                    filter = builder.Eq(e => e.MMIv8Model.DocumentId, SourceEntityModelHash);

                if (validOnly)
                    filter &= builder.Exists(m => m.TecDocModel.DocumentId)
                            & builder.Exists(m => m.MMIv8Model.DocumentId)
                            & builder.Ne(x => x.Status.Current.Status, Status.Deprecated);
                var result = await base.GetFindFluent(filter: filter).ToListAsync();

                return result is not null ? result : Error.NoContent("MatchMakeModel.NotFoundByModelId", $"No MatchMakeModel records exist for {sourceIndex} {SourceEntityModelHash}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving MatchMakeModel {@sourceIndex} {@SourceEntityModelHash}", sourceIndex, SourceEntityModelHash);
                return Error.Failure($"MatchMakeModel.GetByModelIdFailure", $"Error getting Documents for {sourceIndex} {SourceEntityModelHash}. Error: {ex.Message}");
            }
        }

        public async Task<Result<MatchMakeModel>> GetByModelIds(string? TD_SourceEntityModelHash = null, string? MMI_SourceEntityModelHash = null)
        {
            try
            {
                var builder = Builders<MatchMakeModel>.Filter;
                var filter = builder.Empty;

                if (TD_SourceEntityModelHash is not null && TD_SourceEntityModelHash != string.Empty)
                    filter &= builder.Eq(e => e.TecDocModel.DocumentId, TD_SourceEntityModelHash);
                else
                    filter &= builder.Exists(e => e.TecDocModel.DocumentId, false);
                

                if (MMI_SourceEntityModelHash is not null && MMI_SourceEntityModelHash != string.Empty)
                    filter &= builder.Eq(e => e.MMIv8Model.DocumentId, MMI_SourceEntityModelHash);
                else
                    filter &= builder.Exists(e => e.MMIv8Model.DocumentId, false);
                
                var result = await base.GetFindFluent(filter: filter).FirstOrDefaultAsync();

                return result is not null ? result : Error.NoContent("MatchMakeModel.NotFoundByModelIds", $"No MatchMakeModel records exist for TD {TD_SourceEntityModelHash} and MMI {MMI_SourceEntityModelHash}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving MatchMakeModel {@TD_SourceEntityModelHash} and {@MMI_SourceEntityModelHash}", TD_SourceEntityModelHash, MMI_SourceEntityModelHash);
                return Error.Failure($"MatchMakeModel.GetByModelIdsFailure", $"Error getting Documents for TD {TD_SourceEntityModelHash} and MMI {MMI_SourceEntityModelHash}. Error: {ex.Message}");
            }
        }

        public async Task<bool> CheckValid(MongoSourceEntityModel tecdoc, MongoSourceEntityModel mmiv8)
        {
            var builder = Builders<MatchMakeModel>.Filter;
            var filter = builder.Eq(m => m.TecDocModel.DocumentId, tecdoc.DocumentId) 
                       & builder.Eq(m => m.MMIv8Model.DocumentId, mmiv8.DocumentId)
                       & builder.Ne(x => x.Status.Current.Status, Status.Deprecated);

            bool match = await base.GetFindFluent(filter).FirstOrDefaultAsync() is not null;

            return match;
        }

        public new async Task<Result> Create(MatchMakeModel model)
        {
            var createResult = await base.Create(model);
            if (!createResult.IsSuccess)
                return createResult;

            var updateResult = await UpdateRelatedMatches(model, $"Added match {model.DocumentId.ToString()}");
            if (!updateResult.IsSuccess)
                return updateResult;

            return Result.Success();
        }

        private async Task<Result<UpdateResult>> UpdateRelatedMatches(MatchMakeModel model, string? detail = null)
        {
            var filterBuilder = Builders<MatchMakeModel>.Filter;

            var tecdocMatchesResult = await GetByModelId(SourceIndex.TecDocPC, model.TecDocModel.DocumentId);
            if (!tecdocMatchesResult.IsSuccess && tecdocMatchesResult.Error!.Type != ErrorType.NoContent)
                return tecdocMatchesResult.Error!;

            var mmiMatchesResult = await GetByModelId(SourceIndex.MMIv8, model.MMIv8Model.DocumentId);
            if (!mmiMatchesResult.IsSuccess && mmiMatchesResult.Error!.Type != ErrorType.NoContent)
                return mmiMatchesResult.Error!;

            var updateFilter = filterBuilder.Exists(m => m.TecDocModel.DocumentId) & filterBuilder.Exists(m => m.MMIv8Model.DocumentId);
            var tecdocFilter = filterBuilder.In(m => m.TecDocModel.DocumentId, tecdocMatchesResult.Value.Select(m => m.TecDocModel.DocumentId));
            var mmiFilter = filterBuilder.In(m => m.MMIv8Model.DocumentId, mmiMatchesResult.Value.Select(m => m.MMIv8Model.DocumentId));

            updateFilter &= tecdocFilter | mmiFilter;

            return await base.UpdateStatus(updateFilter, Status.Check, detail);
        }
    }
}
