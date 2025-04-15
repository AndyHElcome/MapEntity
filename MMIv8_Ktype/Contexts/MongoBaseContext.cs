using MongoDB.Driver;
using Serilog;
using System.Diagnostics;

namespace MMIv8_Ktype.Core.Contexts
{
    public class MongoBaseContext(MongoDBContext MMIv8_Ktype)
    {
        public async Task<IAsyncCursor<T>> GetCursor<T>(IMongoCollection<T> collection, FilterDefinition<T>? filter = null, SortDefinition<T>? sort = null, int? skip = null, int? take = null, int? batchSize = null)
        {
            filter ??= Builders<T>.Filter.Empty;

            FindOptions<T> options = new()
            {
                BatchSize = batchSize,
                Skip = skip,
                Limit = take,
            };

            if (sort is not null)
                options.Sort = sort;

            return await collection.FindAsync(filter, options);
        }

        public async Task<T?> GetSingleDocument<T>(IMongoCollection<T> collection, FilterDefinition<T>? filter = null, SortDefinition<T>? sort = null)
        {
            var result = await GetCursor(collection, filter, sort, take: 1);

            return result.FirstOrDefault();
        }

        public async Task<IEnumerable<T>> GetMultipleDocuments<T>(IMongoCollection<T> collection, FilterDefinition<T>? filter = null, SortDefinition<T>? sort = null)
        {
            var result = await GetCursor(collection, filter, sort);

            return await result.ToListAsync();
        }

        public async IAsyncEnumerable<IEnumerable<T>> EnumerateDocuments<T>(IMongoCollection<T> collection, FilterDefinition<T> filter, int batchSize = 10000)
        {
            var sw = Stopwatch.StartNew();

            Log.Debug("Starting Enumerate {Type}", typeof(T).Name);

            var count = await CountByFilter(collection, filter);

            Log.Information("Enumerating {Count} {Type} {Time}", count, typeof(T).Name, sw);

            int i = 0;

            using var cursor = await GetCursor(collection, filter: filter, batchSize: batchSize);
            while (await cursor.MoveNextAsync())
            {
                yield return cursor.Current;

                i += cursor.Current.Count();
                Log.Information("Processed {Current} of {Total} {Time}", i, count, sw);
            }

            sw.Stop();
            Log.Debug("Completed Enumerate {Type} {Time}", typeof(T).Name, sw);
        }

        public async Task<long> CountByFilter<T>(IMongoCollection<T> collection, FilterDefinition<T> filter)
        {
            return await collection.Find(filter).CountDocumentsAsync();
        }

        public async Task Create<T>(IMongoCollection<T> collection, T newEntity)
        {
            await collection.InsertOneAsync(newEntity);
            Log.Debug("Created {Count} {Type}", 1, typeof(T).Name);
        }

        public async Task Create<T>(IMongoCollection<T> collection, T[] newEntity)
        {
            await collection.InsertManyAsync(newEntity);
            Log.Debug("Created {Count} {Type}", newEntity.Length, typeof(T).Name);
        }

        public async Task<UpdateResult> Update<T>(IMongoCollection<T> collection, FilterDefinition<T> filter, UpdateDefinition<T> update)
        {
            var result = await collection.UpdateManyAsync(filter, update);
            Log.Debug("Updated {Count} {Type}", result.ModifiedCount, typeof(T).Name);
            return result;
        }

        public async Task<T> FindOneAndUpdate<T>(IMongoCollection<T> collection, FilterDefinition<T> filter, UpdateDefinition<T> update)
        {
            var result = await collection.FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<T, T>() { ReturnDocument = ReturnDocument.After });
            Log.Debug("Updated 1 {Type}", typeof(T).Name);
            return result;
        }

        public async Task<DeleteResult> Delete<T>(IMongoCollection<T> collection, FilterDefinition<T> filter)
        {
            var result = await collection.DeleteManyAsync(filter);
            Log.Debug("Deleted {Count} {Type}", result.DeletedCount, typeof(T).Name);
            return result;
        }

        public async Task<ClientBulkWriteResult> BulkWrite(IReadOnlyList<BulkWriteModel> bulkWriteModels)
        {
            var results = await MMIv8_Ktype.Client.BulkWriteAsync(bulkWriteModels);
            Log.Debug("Matched {Count} {Type} Inserted: {Inserted} Upserted: {Upserted} Modified: {Modified} Deleted: {Deleted}", results.MatchedCount, "unknown", results.InsertedCount, results.UpsertedCount, results.ModifiedCount, results.DeletedCount);
            return results;
        }

    }
}
