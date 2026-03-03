using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Models;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Serilog;
using System.Diagnostics;

namespace MMIv8_Ktype.Core.Services
{
    public interface ICombinationPipeline
    {
        Task<Result<UpdateResultDTO>> UpdateDocuments();
        BulkWriteModel ToBulkWriteModel();
    }

    public class CombinationPipeline<T> : ICombinationPipeline
    {
        public IMongoCollection<T> Collection { get; }
        public FilterDefinition<T> Filter { get; }
        public PipelineDefinition<T, T> Pipeline { get => pipeline; }
        public UpdateDefinition<T> Update => Builders<T>.Update.Pipeline(Pipeline);
        public UpdateOptions<T>? UpdateOptions { get; set; }

        private PipelineDefinition<T, T> pipeline = new EmptyPipelineDefinition<T>();
        private RenderArgs<T> renderArgs = new (BsonSerializer.SerializerRegistry.GetSerializer<T>(), BsonSerializer.SerializerRegistry);

        public CombinationPipeline(IMongoCollection<T> collection, FilterDefinition<T> filter)
        {
            Collection = collection;
            Filter = filter;
        }

        public CombinationPipeline(IMongoCollection<T> collection, FilterDefinition<T> filter, PipelineDefinition<T, T> pipeline)
        {
            Collection = collection;
            Filter = filter;
            _ = SetPipeline(pipeline);
        }

        public CombinationPipeline(IMongoCollection<T> collection, FilterDefinition<T> filter, Func<PipelineDefinition<T, T>, PipelineDefinition<T, T>> func)
        {
            Collection = collection;
            Filter = filter;
            _ = AppendPipeline(func);
        }

        public CombinationPipeline<T> AppendPipeline(Func<PipelineDefinition<T, T>, PipelineDefinition<T, T>> func)
        {
            pipeline = func(pipeline);
            return this;
        }

        public CombinationPipeline<T> AppendUpdate(Func<UpdateDefinition<T>, UpdateDefinition<T>> func)
        {
            var rendered = func(Builders<T>.Update.Combine()).Render(renderArgs);

            pipeline = pipeline.AppendStage<T, T, T>(rendered.AsBsonDocument);
            return this;
        }

        private CombinationPipeline<T> SetPipeline(PipelineDefinition<T, T> pipeline)
        {
            this.pipeline = pipeline;
            return this;
        }

        public async Task<Result<UpdateResultDTO>> UpdateDocuments()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await Collection.UpdateManyAsync(Filter, Update);
                Log.Debug("Updated {Count} {Type} in {time}", result.ModifiedCount, typeof(T).Name, sw);
                return result ?? new UpdateResultDTO();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error Updating {Type} from filter: {filter}", typeof(T).Name, Filter.ToString());
                return Error.Failure($"{typeof(T)}.UpdateFailure", $"Could not update documents. Error: {ex.Message}");
            }
        }

        public async Task<Result<T>> FindAndUpdateDocument(bool afterUpdate = true)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var updateOptions = UpdateOptions?.ConvertToFindOneAndUpdateOptions() ?? new FindOneAndUpdateOptions<T>();

                if (afterUpdate)
                    updateOptions.ReturnDocument = ReturnDocument.After;

                var result = await Collection.FindOneAndUpdateAsync(Filter, Update, updateOptions);

                if (result is null)
                    return Error.NotFound($"{typeof(T).Name}.NotFoundForUpdate", $"Could not find and update document");

                Log.Debug("Updated 1 {Type} in {time}", typeof(T).Name, sw);
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error Updating {Type} from filter: {filter}", typeof(T).Name, Filter.ToString());
                return Error.Failure($"{typeof(T)}.FindAndUpdateDocumentFailure", $"Could not find and update document. Error: {ex.Message}");
            }
        }

        public BulkWriteModel ToBulkWriteModel() => this;

        public static implicit operator BulkWriteModel(CombinationPipeline<T> combinationPipeline)
            => new BulkWriteUpdateManyModel<T>(
                combinationPipeline.Collection.CollectionNamespace,
                combinationPipeline.Filter,
                combinationPipeline.Update,
                combinationPipeline.UpdateOptions?.Collation,
                combinationPipeline.UpdateOptions?.Hint,
                combinationPipeline.UpdateOptions?.IsUpsert ?? false);
    }

    public class BulkCombinationUpdate
    {
        public MongoDBContext MMIv8_Ktype { get; }
        public BulkWriteOptions? WriteOptions { get; set; }
        public List<BulkWriteModel> BulkWriteModels { get => combinationPipelines.Select(c => c.ToBulkWriteModel()).ToList(); }

        public int Count() => combinationPipelines.Count;
        private readonly List<ICombinationPipeline> combinationPipelines = [];

        public BulkCombinationUpdate(MongoDBContext mmiv8_Ktype)
        {
            MMIv8_Ktype = mmiv8_Ktype;
        }

        public BulkCombinationUpdate(MongoDBContext mmiv8_Ktype, BulkWriteOptions writeOptions)
        {
            MMIv8_Ktype = mmiv8_Ktype;
            WriteOptions = writeOptions;
        }

        public BulkCombinationUpdate AddCombinationUpdate(ICombinationPipeline combinationPipeline)
        {
            combinationPipelines.Add(combinationPipeline);
            return this;
        }

        public BulkCombinationUpdate AddCombinationUpdate(IEnumerable<ICombinationPipeline> combinationPipelines)
        {
            foreach (var combinationPipeline in combinationPipelines)
            {
                _ = AddCombinationUpdate(combinationPipeline);
            }
            return this;
        }

        public BulkCombinationUpdate AddCombinationUpdate(params ICombinationPipeline[] combinationPipelines)
        {
            return AddCombinationUpdate((IEnumerable<ICombinationPipeline>)combinationPipelines);
        }

        public BulkCombinationUpdate Combine(BulkCombinationUpdate bulkCombinationPipeline)
        {
            return AddCombinationUpdate(bulkCombinationPipeline.combinationPipelines);
        }

        public async Task<Result<ClientBulkWriteResult>> CommitBulkWrite()
        {
            var sw = Stopwatch.StartNew();

            if (BulkWriteModels.Count == 0)
                return Error.Validation("ClientBulkWriteResult.NoBulkWriteModels", "No Bulk write models present to commit");

            try
            {
                var results = await MMIv8_Ktype.Client.BulkWriteAsync(BulkWriteModels);
                Log.Debug("Matched {Count} {Type} Inserted: {Inserted} Upserted: {Upserted} Modified: {Modified} Deleted: {Deleted} in {time}", results.MatchedCount, "typeof(T)", results.InsertedCount, results.UpsertedCount, results.ModifiedCount, results.DeletedCount, sw);
                return results;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in ClientBulkWriteResult");
                return Error.Failure("ClientBulkWriteResult.UpdateFailure", $"Could not update documents. Error: {ex.Message}");
            }
        }

        public async void CommitInParallel()
        {
            await Task.Run(() => combinationPipelines.ForEach(c => c.UpdateDocuments()));

            //Parallel.ForEach(combinationPipelines, c => c.UpdateDocuments());
        }

    }

    public static class CombinationUpdateExtensions
    {
        public static FindOneAndUpdateOptions<T> ConvertToFindOneAndUpdateOptions<T>(this UpdateOptions<T> updateOptions)
        {
            return new FindOneAndUpdateOptions<T>()
            {
                ArrayFilters = updateOptions.ArrayFilters,
                BypassDocumentValidation = updateOptions.BypassDocumentValidation,
                Collation = updateOptions.Collation,
                Comment = updateOptions.Comment,
                Hint = updateOptions.Hint,
                IsUpsert = updateOptions.IsUpsert,
                Sort = updateOptions.Sort,
            };
        }
    }
}
