using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Models.Collections;
using MongoDB.Driver;
using MongoDB.Bson;
using MMIv8_Ktype.Models.Indexes;
using System.Reflection.Metadata;
using MMIv8_Ktype.Api.Responses;
using Serilog;
using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using MongoDB.Driver.Linq;

namespace MMIv8_Ktype.Core.Services
{
    public class BaseService<T, Tid>(IMongoCollection<T> Collection)
        where T : ICollectionEntity<Tid>
    {
        public IMongoCollection<T> Collection = Collection;

        public SortDefinition<T> GetSortById() => Builders<T>.Sort.Ascending(c => c.DocumentId);

        public FilterDefinition<T> GetFilterById(Tid documentId) => Builders<T>.Filter.Eq(c => c.DocumentId, documentId);

        #region Query
        public IQueryable<T> GetQuery()
        {
            return Collection.AsQueryable();
        }

        public async Task<long> CountByFilter(FilterDefinition<T>? filter = null)
        {
            filter ??= Builders<T>.Filter.Empty;
            return await Collection.Find(filter).Sort(this.GetSortById()).CountDocumentsAsync();
        }
        #endregion

        #region Get Documents
        public async Task<IAsyncCursor<TOut>> GetCursor<TOut>(FilterDefinition<T>? filter = null, SortDefinition<T>? sort = null, int? skip = null, int? take = null, int? batchSize = null, ProjectionDefinition<T, TOut>? projection = null)
        {
            filter ??= Builders<T>.Filter.Empty;

            FindOptions<T, TOut> options = new()
            {
                BatchSize = batchSize,
                Skip = skip,
                Limit = take,
                Sort = this.GetSortById(), //TODO Check this is slowing down queries
            };

            if (sort is not null)
                options.Sort = sort;

            if (projection is not null)
                options.Projection = projection;

            return await Collection.FindAsync(filter, options);
        }

        public async Task<IAsyncCursor<T>> GetCursor(FilterDefinition<T>? filter = null, SortDefinition<T>? sort = null, int? skip = null, int? take = null, int? batchSize = null, ProjectionDefinition<T, T>? projection = null)
        {
            return await this.GetCursor<T>(filter, sort, skip, take, batchSize, projection);
        }

        public async Task<IAsyncCursor<TOut>> GetDistinctCursor<TOut>(string fieldName, FilterDefinition<T>? filter = null)
        {
            filter ??= Builders<T>.Filter.Empty;
            return await Collection.DistinctAsync<TOut>(fieldName, filter);
        }

        public async Task<TOut> GetSingleDocument<TOut>(FilterDefinition<T>? filter = null, SortDefinition<T>? sort = null, int? skip = null, ProjectionDefinition<T, TOut>? projection = null)
        {
            var result = await this.GetCursor(filter, sort, skip, take: 1, projection: projection);

            return await result.FirstOrDefaultAsync();
        }

        public async Task<T> GetSingleDocument(FilterDefinition<T>? filter = null, SortDefinition<T>? sort = null, int? skip = null, ProjectionDefinition<T, T>? projection = null)
        {
            return await this.GetSingleDocument<T>(filter, sort, skip, projection: projection);
        }

        public async Task<T> GetById(Tid documentId)
        {
            return await this.GetSingleDocument( this.GetFilterById(documentId));
        }

        public async Task<List<T>> GetMultipleDocuments(FilterDefinition<T>? filter = null, SortDefinition<T>? sort = null)
        {
            var result = await this.GetCursor<T>(filter, sort);

            return await result.ToListAsync();
        }

        public async Task<PagedResponse<TOut>> PaginateDocuments<TOut>(FilterDefinition<T>? filter = null, SortDefinition<T>? sort = null, int page = 1, int pageSize = 100, ProjectionDefinition<T, TOut>? projection = null)
        {
            var sw = Stopwatch.StartNew();

            var count = await this.CountByFilter(filter);

            Log.Debug("Starting Enumerate {Type} count took {Time}", typeof(T).Name, sw);

            Log.Information("Paging {Type} page: {page}", typeof(T).Name, page - 1);

            sort = sort is null ? this.GetSortById() : sort.Ascending(c => c.DocumentId);
            var result = await this.GetCursor<TOut>(filter, sort, (page - 1) * pageSize, pageSize, projection: projection);

            var items = await result.ToListAsync();

            sw.Stop();
            Log.Debug("Completed Enumerate of {count} {Type} in {Time}", items.Count, typeof(T).Name, sw);

            return new PagedResponse<TOut>(items, Convert.ToInt32(count), page, pageSize);
        }

        public async IAsyncEnumerable<IEnumerable<TOut>> EnumerateDocuments<TOut>(FilterDefinition<T> filter, int batchSize = 10000, ProjectionDefinition<T, TOut>? projection = null)
        {
            var sw = Stopwatch.StartNew();

            Log.Debug("Starting Enumerate {Type}", typeof(T).Name);

            var count = await this.CountByFilter(filter);

            Log.Information("Enumerating {Count} {Type} {Time}", count, typeof(T).Name, sw);

            int i = 0;

            using var cursor = await this.GetCursor<TOut>(filter: filter, batchSize: batchSize);
            while (await cursor.MoveNextAsync())
            {
                yield return cursor.Current;

                i += cursor.Current.Count();
                Log.Information("Processed {Current} of {Total} {Time}", i, count, sw);
            }

            sw.Stop();
            Log.Debug("Completed Enumerate {Type} {Time}", typeof(T).Name, sw);
        }
        #endregion

        #region Creation 
        //TODO Look into replace or upsert creations?
        public async Task Create(T newEntity)
        {
            await Collection.InsertOneAsync(newEntity);
            Log.Debug("Created {Count} {Type}", 1, typeof(T).Name);
        }

        public async Task CreateAndValidate(T newEntity)
        {
            if (await this.GetById(newEntity.DocumentId) is not null)
            {
                Log.Information("Document already exists {type} with Id of {DocumentId}", typeof(T), newEntity.DocumentId?.ToString());
                return;
            }

            await this.Create(newEntity);
        }

        public async Task Create(T[] newEntity)
        {
            await Collection.InsertManyAsync(newEntity);
            Log.Debug("Created {Count} {Type}", newEntity.Length, typeof(T).Name);
        }
        #endregion

        #region Update
        [Obsolete("UseComboUpdate")]
        public async Task<UpdateResult> Update(FilterDefinition<T> filter, UpdateDefinition<T> update)
        {
            var result = await Collection.UpdateManyAsync(filter, update);
            Log.Debug("Updated {Count} {Type}", result.ModifiedCount, typeof(T).Name);
            return result;
        }

        [Obsolete("UseComboUpdate")]
        public async Task<T> FindOneAndUpdate(FilterDefinition<T> filter, UpdateDefinition<T> update)
        {
            var result = await Collection.FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<T, T>() { ReturnDocument = ReturnDocument.After });
            Log.Debug("Updated 1 {Type}", typeof(T).Name);
            return result;
        }
        #endregion

        #region Deletion
        public async Task<DeleteResult> DeleteByFilter(FilterDefinition<T> filter)
        {
            var result = await Collection.DeleteManyAsync(filter);
            Log.Debug("Deleted {Count} {Type}", result.DeletedCount, typeof(T).Name);
            return result;
        }

        public async Task<DeleteResult> DeleteAll()
        {
            var filter = Builders<T>.Filter.Empty;
            return await this.DeleteByFilter(filter);
        }

        public async Task<T> FindOneAndDelete(FilterDefinition<T> filter)
        {
            var result = await Collection.FindOneAndDeleteAsync(filter);
            Log.Debug("Deleted 1 {Type}", typeof(T).Name);
            return result;
        }

        public async Task<T> DeleteById(Tid documentId)
        {
            return await this.FindOneAndDelete(this.GetFilterById(documentId));
        }

        public async Task<T> Delete(T document)
        {
            return await this.DeleteById(document.DocumentId);
        }

        public async Task Delete(T[] documents)
        {
            foreach (var document in documents)
            {
                _ = await this.DeleteById(document.DocumentId);
            }
        }
        #endregion
    }
}
