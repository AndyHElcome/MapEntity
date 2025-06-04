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
using System.Xml.Linq;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Core.Services.Source;
using System.Net.NetworkInformation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MMIv8_Ktype.Core.Services;
using MMIv8_Ktype.Models.Util;
using System.Data.SqlTypes;

namespace MMIv8_Ktype.Core.Services
{
    public class BaseService<T, Tid>(IMongoCollection<T> Collection)
        where T : ICollectionEntity<Tid?>
    {
        public IMongoCollection<T> Collection = Collection;

        private protected SortDefinition<T> SortByDocumentId(SortDefinition<T>? sort = null) => sort is null ? Builders<T>.Sort.Ascending(c => c.DocumentId) : sort;
        private protected FilterDefinition<T> FilterByDocumentId(Tid documentId) => Builders<T>.Filter.Eq(c => c.DocumentId, documentId);
        private protected FilterDefinition<T> FilterGtDocumentId(Tid documentId) => Builders<T>.Filter.Gt(c => c.DocumentId, documentId);
        private protected FilterDefinition<T> FilterLteDocumentId(Tid documentId) => Builders<T>.Filter.Lte(c => c.DocumentId, documentId);

        #region Query
        internal IFindFluent<T, T> GetFindFluent(FilterDefinition<T>? filter = null, SortDefinition<T>? sort = null, int? batchSize = null)
        {
            filter ??= Builders<T>.Filter.Empty;
            var options = new FindOptions { BatchSize = batchSize };

            return Collection.Find(filter, options).Sort(this.SortByDocumentId(sort));
        }

        internal IQueryable<T> GetQueryable()
        {
            return Collection.AsQueryable();
        }

        internal async Task<long> CountByFilter(FilterDefinition<T>? filter = null)
        {
            if (filter is null || filter == Builders<T>.Filter.Empty)
            {
                filter = Builders<T>.Filter.Empty;
                var opt = new CountOptions() { Hint = "_id_" };
                return await Collection.CountDocumentsAsync(filter, opt);
            }
            else
            {
                var projection = Builders<T>.Projection.Include(c => c.DocumentId);
                return await Collection.Find(filter).Project(projection).CountDocumentsAsync();
            }
        }

        #endregion

        #region Get Documents

        private protected async Task<IAsyncCursor<TOut>> GetDistinctCursor<TOut>(string fieldName, FilterDefinition<T>? filter = null)
        {
            filter ??= Builders<T>.Filter.Empty;

            return await Collection.DistinctAsync<TOut>(fieldName, filter);
        }

        public async Task<List<TOut>> GetDistinctDocuments<TOut>(string fieldName, FilterDefinition<T>? filter = null)
        {
            filter ??= Builders<T>.Filter.Empty;

            Log.Debug("Starting Get Distinct {Type}", typeof(T).Name);
            var sw = Stopwatch.StartNew();

            var results = await Collection.Distinct<TOut>(fieldName, filter).ToListAsync();

            sw.Stop();
            Log.Information("Completed Get of {count} Distinct {Type} into {OutType} in {Time}", results.Count, typeof(T).Name, typeof(TOut).Name, sw);
            return results;
        }

        public async IAsyncEnumerable<IEnumerable<TOut>> EnumerateDistinctDocuments<TOut>(string fieldName, FilterDefinition<T>? filter = null)
        {
            filter ??= Builders<T>.Filter.Empty;

            Log.Debug("Starting Enumerate Distinct {Type}", typeof(T).Name);
            var sw = Stopwatch.StartNew();

            int i = 0;
            using var cursor = await this.GetDistinctCursor<TOut>(fieldName, filter);
            while (await cursor.MoveNextAsync())
            {
                yield return cursor.Current;

                i += cursor.Current.Count();
                Log.Debug("Enumerating Distinct {Type} {Current} {Time}", typeof(T).Name, i, sw);
            }

            sw.Stop();
            Log.Information("Completed Enumerate of {count} Distinct {Type} into {OutType} in {Time}", i, typeof(T).Name, typeof(TOut).Name, sw);
        }

        public async Task<T> GetById(Tid documentId)
        {
            return await this.GetFindFluent(this.FilterByDocumentId(documentId)).FirstOrDefaultAsync();
        }

        public async Task<PagedResponse<TOut>> PaginateDocuments<TOut>(FilterDefinition<T>? filter = null, SortDefinition<T>? sort = null, int page = 1, int pageSize = 100, ProjectionDefinition<T, TOut>? projection = null)
        {
            Log.Debug("Paging {Type} Page: {page}", typeof(T).Name, page);
            var sw = Stopwatch.StartNew();

            var count = this.CountByFilter(filter);

            var results = this.GetFindFluent(filter, sort)
                              .Skip((page - 1) * pageSize)
                              .Limit(pageSize)
                              .Project(projection)
                              .ToListAsync();

            var pagedResults = new PagedResponse<TOut>(await results, Convert.ToInt32(await count), page, pageSize);

            sw.Stop();
            Log.Information("Paged {Type} {@page} in {Time}", typeof(T).Name, pagedResults.PageDetails(), sw);

            return pagedResults;
        }

        public async Task<PagedCursorResponse<TOut>> PaginateDocumentsByCursor<TOut, TOutId>(FilterDefinition<T>? filter = null, Tid? cursor = default, int pageSize = 100, ProjectionDefinition<T, TOut>? projection = null)
            where TOut : ICollectionEntity<TOutId>
        {
            Log.Debug("Paging {Type} after: {@cursor}", typeof(T).Name, cursor?.ToString() ?? string.Empty);
            var sw = Stopwatch.StartNew();

            var count = this.CountByFilter(filter);
            long preCount = 1;

            if (cursor is not null)
            {
                if (filter is null)
                {
                    preCount = await this.CountByFilter(this.FilterLteDocumentId(cursor));
                    filter = this.FilterGtDocumentId(cursor);
                }
                else
                {
                    preCount = await this.CountByFilter(this.FilterLteDocumentId(cursor) & filter);
                    filter = this.FilterGtDocumentId(cursor) & filter;

                }
            }
            
            var results = await this.GetFindFluent(filter)
                                    .Limit(pageSize)
                                    .Project(projection)
                                    .ToListAsync();

            var pre = preCount == 0 ? 1 : (Convert.ToInt32(preCount) / pageSize) + 1; //TODO review this
            var pagedResults = new PagedCursorResponse<TOut>(results, Convert.ToInt32(await count), pre, pageSize, results.LastOrDefault()?.DocumentId?.ToString() ?? string.Empty);

            sw.Stop();
            Log.Information("Paged {Type} {@page} in {Time}", typeof(T).Name, pagedResults.PageDetails(), sw);

            return pagedResults;
        }

        public async IAsyncEnumerable<IEnumerable<TOut>> EnumerateDocuments<TOut>(FilterDefinition<T> filter, int batchSize = 10000, ProjectionDefinition<T, TOut>? projection = null)
        {
            Log.Debug("Starting Enumerate {Type}", typeof(T).Name);
            var sw = Stopwatch.StartNew();

            var query = this.GetFindFluent(filter: filter, batchSize: batchSize);

            var count = await query.CountDocumentsAsync();
            int i = 0;
            using var cursor = await query.Project(projection).ToCursorAsync();
            while (await cursor.MoveNextAsync())
            {
                yield return cursor.Current;

                i += cursor.Current.Count();
                Log.Debug("Enumerating {Type} {Current} of {Total} {Time}", typeof(T).Name, i, count, sw);
            }

            sw.Stop();
            Log.Information("Completed Enumerate {Type} {Time}", typeof(T).Name, sw);
        }
        #endregion

        #region Creation 
        //TODO Look into replace or upsert creations?
        public async Task<Result> Create(T document)
        {
            try
            {
                await Collection.InsertOneAsync(document);
                Log.Debug("Created {Count} {Type}", 1, typeof(T).Name);
                return Result.Success();
            }
            catch (Exception ex)
            {
                Log.Error("Error Creating {Type} {@document}", typeof(T).Name, document);
                return Error.Failure($"{typeof(T)}.CreationFailure", $"Could not create {@document}. Error: {ex.Message}");
            }
        }

        public async Task CreateAndValidate(T document)
        {
            if (await this.GetById(document.DocumentId) is not null)
            {
                Log.Information("Document already exists {type} with Id of {DocumentId}", typeof(T), document.DocumentId?.ToString());
                return;
            }

            await this.Create(document);
        }

        public async Task Create(T[] documents)
        {
            await Collection.InsertManyAsync(documents);
            Log.Debug("Created {Count} {Type}", documents.Length, typeof(T).Name);
        }
        #endregion

        #region Update

        public CombinationPipeline<TUpdate> Update<TUpdate>(FilterDefinition<TUpdate> filter)
           where TUpdate : T
        {
            return new CombinationPipeline<TUpdate>((IMongoCollection<TUpdate>)Collection, filter);
        }

        public CombinationPipeline<TUpdate> Update<TUpdate>(TUpdate document)
            where TUpdate : T
        {
            var filter = Builders<TUpdate>.Filter.Eq(c => c.DocumentId, document.DocumentId);

            return this.Update(filter);
        }

        public CombinationPipeline<T> Update(Tid documentId)
        {
            var filter = this.FilterByDocumentId(documentId);

            return this.Update(filter);
        }


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

        public async Task<Result<DeleteResult>> DeleteAll()
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
            return await this.FindOneAndDelete(this.FilterByDocumentId(documentId));
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

    public class BaseServiceWithVersion<T, Tid>(IMongoCollection<T> Collection, IVersionProvider versionProvider) : BaseService<T, Tid>(Collection)
        where T : ICollectionEntity<Tid>, IStatusHistory
    {
        public IVersionProvider VersionProvider = versionProvider;

        public async Task<UpdateResult?> UpdateStatus(T document, Status status, string? detail = null)
        {
            return await this.Update(document).AppendPipeline(c => c.AppendStatus(VersionProvider.NewStatus(status, detail))).UpdateDocuments();
        }

        public async Task<UpdateResult?> UpdateStatus(FilterDefinition<T> filter, Status status, string? detail = null)
        {
            return await this.Update(filter).AppendPipeline(c => c.AppendStatus(VersionProvider.NewStatus(status, detail))).UpdateDocuments();
        }
    }

    public class BaseServiceWithDifferences<T, Tid>(IMongoCollection<T> Collection, IVersionProvider versionProvider) : BaseServiceWithVersion<T, Tid>(Collection, versionProvider)
        where T : ICollectionEntity<Tid>, IStatusHistory, IUpdateDifferences
    {
        public async Task<T> UpdateDifferences(T document, T newDocument)
        {
            string? differences = null;
            return await this.Update(document).AppendUpdate(c => c.UpdateDifferences(document, newDocument, out differences))
                                              .AppendPipeline(c => c.AppendStatus(VersionProvider.NewStatus(Status.Updated, $"Entity Updated: '{differences}'")))
                                              .FindAndUpdateDocument();
        }
    }
}
