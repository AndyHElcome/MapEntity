using Microsoft.Extensions.Options;
using MMIv8_Ktype.Api.Responses;
using MongoDB.Driver;
using Serilog;
using System.Diagnostics;

namespace MMIv8_Ktype.Core.Contexts
{
    public class MongoBaseContext(MongoDBContext MMIv8_Ktype)
    {
        public async Task<IAsyncCursor<TOut>> GetCursor<T, TOut>(IMongoCollection<T> collection, FilterDefinition<T>? filter = null, SortDefinition<T>? sort = null, int? skip = null, int? take = null, int? batchSize = null, ProjectionDefinition<T, TOut>? projection = null)
        {
            filter ??= Builders<T>.Filter.Empty;

            FindOptions<T, TOut> options = new()
            {
                BatchSize = batchSize,
                Skip = skip,
                Limit = take,
            };

            if (sort is not null)
                options.Sort = sort;

            if (projection is not null)
                options.Projection = projection;

            return await collection.FindAsync(filter, options);
        }

        public async Task<IAsyncCursor<T>> GetCursor<T>(IMongoCollection<T> collection, FilterDefinition<T>? filter = null, SortDefinition<T>? sort = null, int? skip = null, int? take = null, int? batchSize = null, ProjectionDefinition<T, T>? projection = null)
        {
            return await GetCursor<T,T>(collection, filter, sort, skip, take, batchSize, projection);
        }

        public async Task<TOut> GetSingleDocument<T, TOut>(IMongoCollection<T> collection, FilterDefinition<T>? filter = null, SortDefinition<T>? sort = null, int? skip = null, ProjectionDefinition<T, TOut>? projection = null)
        {
            var result = await GetCursor(collection, filter, sort, skip, take: 1, projection: projection);

            return await result.FirstOrDefaultAsync();
        }

        public async Task<T> GetSingleDocument<T>(IMongoCollection<T> collection, FilterDefinition<T>? filter = null, SortDefinition<T>? sort = null, int? skip = null, ProjectionDefinition<T, T>? projection = null)
        {
            return await GetSingleDocument<T, T>(collection, filter, sort, skip, projection: projection);
        }

        public async Task<IEnumerable<T>> GetMultipleDocuments<T>(IMongoCollection<T> collection, FilterDefinition<T>? filter = null, SortDefinition<T>? sort = null)
        {
            var result = await GetCursor<T, T>(collection, filter, sort);

            return await result.ToListAsync();
        }

        public async Task<PagedResponse<T>> PaginateDocuments<T>(IMongoCollection<T> collection, SortDefinition<T> sort, FilterDefinition<T>? filter = null, int page = 1, int pageSize = 100)
        {
            var sw = Stopwatch.StartNew();

            var count = await CountByFilter(collection, filter);

            Log.Debug("Starting Enumerate {Type} count took {Time}", typeof(T).Name, sw);

            Log.Information("Paging {Type} page: {page}", typeof(T).Name, page - 1);

            var result = await GetCursor<T, T>(collection, filter, sort, (page - 1) * pageSize, pageSize);

            var items = await result.ToListAsync();

            sw.Stop();
            Log.Debug("Completed Enumerate of {count} {Type} in {Time}", items.Count, typeof(T).Name, sw);

            return new PagedResponse<T>(items, Convert.ToInt32(count), page, pageSize);
        }

        public async IAsyncEnumerable<IEnumerable<T>> EnumerateDocuments<T>(IMongoCollection<T> collection, FilterDefinition<T> filter, int batchSize = 10000)
        {
            var sw = Stopwatch.StartNew();

            Log.Debug("Starting Enumerate {Type}", typeof(T).Name);

            var count = await CountByFilter(collection, filter);

            Log.Information("Enumerating {Count} {Type} {Time}", count, typeof(T).Name, sw);

            int i = 0;

            using var cursor = await GetCursor<T, T>(collection, filter: filter, batchSize: batchSize);
            while (await cursor.MoveNextAsync())
            {
                yield return cursor.Current;

                i += cursor.Current.Count();
                Log.Information("Processed {Current} of {Total} {Time}", i, count, sw);
            }

            sw.Stop();
            Log.Debug("Completed Enumerate {Type} {Time}", typeof(T).Name, sw);
        }

        public IQueryable<T> GetQuery<T>(IMongoCollection<T> collection)
        {
            return collection.AsQueryable();
        }

        public async Task<long> CountByFilter<T>(IMongoCollection<T> collection, FilterDefinition<T>? filter = null)
        {
            filter ??= Builders<T>.Filter.Empty;
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

        [Obsolete("UseComboUpdate")]
        public async Task<UpdateResult> Update<T>(IMongoCollection<T> collection, FilterDefinition<T> filter, UpdateDefinition<T> update)
        {
            var result = await collection.UpdateManyAsync(filter, update);
            Log.Debug("Updated {Count} {Type}", result.ModifiedCount, typeof(T).Name);
            return result;
        }

        [Obsolete("UseComboUpdate")]
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

        public async Task<T> FindOneAndDelete<T>(IMongoCollection<T> collection, FilterDefinition<T> filter)
        {
            var result = await collection.FindOneAndDeleteAsync(filter);
            Log.Debug("Deleted 1 {Type}", typeof(T).Name);
            return result;
        }
    }
}
