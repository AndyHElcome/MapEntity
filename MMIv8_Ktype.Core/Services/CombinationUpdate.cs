using MMIv8_Ktype.Core.Contexts;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Serilog;

namespace MMIv8_Ktype.Core.Services
{
    public interface ICombinationPipeline
    {
        Task<UpdateResult?> UpdateDocuments();

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

        public async Task<UpdateResult?> UpdateDocuments()
        {
            var result = await Collection.UpdateManyAsync(Filter, Update);
            Log.Debug("Updated {Count} {Type}", result.ModifiedCount, typeof(T).Name);
            return result;
        }

        public async Task<T> FindAndUpdateDocument(bool afterUpdate = true)
        {
            var updateOptions = UpdateOptions?.ConvertToFindOneAndUpdateOptions() ?? new FindOneAndUpdateOptions<T>();

            if (afterUpdate)
                updateOptions.ReturnDocument = ReturnDocument.After;

            var result = await Collection.FindOneAndUpdateAsync(Filter, Update, updateOptions);

            if (result is null)
                throw new Exception($"Error Updating {typeof(T).Name}: couldn't find and replace");

            Log.Debug("Updated 1 {Type}", typeof(T).Name);
            return result;
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

        public BulkCombinationUpdate AddCombinationUpdate<T>(params CombinationPipeline<T>[] combinationPipelines)
        {
            return AddCombinationUpdate((IEnumerable<CombinationPipeline<T>>)combinationPipelines);
        }

        public BulkCombinationUpdate AddCombinationUpdate<T>(IEnumerable<CombinationPipeline<T>> combinationPipelines)
        {
            foreach (var combinationPipeline in combinationPipelines)
            {
                _ = AddCombinationUpdate(combinationPipeline);
            }
            return this;
        }

        public BulkCombinationUpdate AddCombinationUpdate<T>(CombinationPipeline<T> combinationPipeline)
        {
            combinationPipelines.Add(combinationPipeline);
            return this;
        }

        public async Task<ClientBulkWriteResult?> CommitBulkWrite()
        {
            if (BulkWriteModels.Count == 0)
                return new ClientBulkWriteResult();

            var results = await MMIv8_Ktype.Client.BulkWriteAsync(BulkWriteModels);
            Log.Debug("Matched {Count} {Type} Inserted: {Inserted} Upserted: {Upserted} Modified: {Modified} Deleted: {Deleted}", results.MatchedCount, "typeof(T)", results.InsertedCount, results.UpsertedCount, results.ModifiedCount, results.DeletedCount);
            return results;
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
