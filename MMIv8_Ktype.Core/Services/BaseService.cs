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
using System.Text.Json;

namespace MMIv8_Ktype.Core.Services
{
    public class BaseService<T, Tid>(IMongoCollection<T> Collection)
        where T : ICollectionEntity<Tid>
    {
        public IMongoCollection<T> Collection = Collection;

        private protected SortDefinition<T> SortByDocumentId(SortDefinition<T>? sort = null) => sort is null ? Builders<T>.Sort.Ascending(c => c.DocumentId) : sort;
        private protected FilterDefinition<T> FilterByDocumentId(Tid documentId) => Builders<T>.Filter.Eq(c => c.DocumentId, documentId);
        private protected FilterDefinition<T> FilterGtDocumentId(Tid documentId) => Builders<T>.Filter.Gt(c => c.DocumentId, documentId);
        private protected FilterDefinition<T> FilterGteDocumentId(Tid documentId) => Builders<T>.Filter.Gte(c => c.DocumentId, documentId);
        private protected FilterDefinition<T> FilterLteDocumentId(Tid documentId) => Builders<T>.Filter.Lte(c => c.DocumentId, documentId);

        #region Query
        internal IFindFluent<T, T> GetFindFluent(FilterDefinition<T>? filter = null, SortDefinition<T>? sort = null, int? batchSize = null)
        {
            filter ??= Builders<T>.Filter.Empty;
            var options = new FindOptions { BatchSize = batchSize };

            if (sort != null)
                return Collection.Find(filter, options).Sort(sort);
            else
                return Collection.Find(filter, options);
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

        public async Task<Result<List<TOut>>> GetDistinctDocuments<TOut>(string fieldName, FilterDefinition<T>? filter = null)
        {
            var sw = Stopwatch.StartNew();
            Log.Debug("Starting Get Distinct {Type}", typeof(T).Name);
            try
            {
                filter ??= Builders<T>.Filter.Empty;
                var results = await Collection.Distinct<TOut>(fieldName, filter).ToListAsync();

                Log.Information("Completed Get of {count} Distinct {Type} into {OutType} in {Time}", results.Count, typeof(T).Name, typeof(TOut).Name, sw);
                return results is { Count: > 0 } ? results : Error.NoContent($"{typeof(T)}.NoContent", $"Could not find any documents");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting distinct Documents for {type} from {field}", typeof(T).Name, fieldName);
                return Error.Failure($"{typeof(T)}.CreationFailure", $"Could not get distinct documents. Error: {ex.Message}");
            }
        }

        private protected async IAsyncEnumerable<IEnumerable<TOut>> EnumerateDistinctDocuments<TOut>(string fieldName, FilterDefinition<T>? filter = null)
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

        public async Task<Result<T>> GetById(Tid documentId)
        {
            try
            {
                var document = await this.GetFindFluent(this.FilterByDocumentId(documentId)).FirstOrDefaultAsync();
                return document is not null ? document : Error.NotFound($"{typeof(T)}.NotFoundByDocumentId", $"No {typeof(T).Name} exists with DocumentId {documentId}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting Documents for {type} with Id {field}", typeof(T).Name, documentId!.ToString());
                return Error.Failure($"{typeof(T)}.GetByIdFailure", $"Error getting Documents with Id {documentId!.ToString()}. Error: {ex.Message}");
            }
        }
        
        public async Task<Result<PagedResponse<TOut>>> PaginateDocuments<TOut>(FilterDefinition<T>? filter = null, SortDefinition<T>? sort = null, int page = 1, int pageSize = 100, ProjectionDefinition<T, TOut>? projection = null)
        {
            try
            {
                Log.Debug("Paging {Type} Page: {page}", typeof(T).Name, page);
                var sw = Stopwatch.StartNew();

                filter ??= Builders<T>.Filter.Empty;

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
            catch (Exception ex)
            {
                Log.Error(ex, "Error Paging Documents for {type}", typeof(T).Name);
                return Error.Failure($"{typeof(T)}.PaginateDocumentsByCursorFailure", $"Error paging Documents. Error: {ex.Message}");
            }
        }

        public async Task<Result<PagedCursorResponse<TOut>>> PaginateDocumentsByCursor<TOut, TOutId>(FilterDefinition<T>? filter = null, Tid? cursor = default, int pageSize = 100, ProjectionDefinition<T, TOut>? projection = null, bool returnResults = true)
            where TOut : ICollectionEntity<TOutId>
            where TOutId : Tid
        {
            try
            {
                Log.Debug("Paging {Type} after: {@cursor}", typeof(T).Name, cursor?.ToString() ?? string.Empty);
                var sw = Stopwatch.StartNew();

                filter ??= Builders<T>.Filter.Empty;

                var count = Convert.ToInt32(await this.CountByFilter(filter));

                if (count == 0) //TODO Fix API digestation of errors
                    //return Error.NoContent($"{typeof(T)}.NoContent", $"Could not find any documents");
                    return new PagedCursorResponse<TOut>([], count, 0, pageSize, string.Empty);

                int precount = 0;
                if (cursor is not null && cursor.ToString() != ObjectId.Empty.ToString())
                {
                    var firstDoc = await this.GetFindFluent(filter)
                                             .Sort(SortByDocumentId())
                                             .Limit(1)
                                             .Project(projection)
                                             .FirstOrDefaultAsync();

                    var precountFilter = this.FilterGteDocumentId(firstDoc.DocumentId) & this.FilterLteDocumentId(cursor) & filter;
                    precount = Convert.ToInt32(await this.CountByFilter(precountFilter));
                }

                int expectedDocuments = count - precount;
                int limit = expectedDocuments < pageSize ? expectedDocuments : pageSize;

                var cursorFilter = cursor is null ? Builders<T>.Filter.Empty : this.FilterGtDocumentId(cursor);
                var query = this.GetFindFluent(cursorFilter & filter)
                                .Sort(SortByDocumentId())
                                .Limit(limit)
                                .Project(projection);
                Log.Debug("Page Query {query}", query.ToString());

                PagedCursorResponse<TOut> pagedResults;
                if (returnResults)
                {
                    var results = await query.ToListAsync();
                    pagedResults = new PagedCursorResponse<TOut>(results, count, precount, pageSize, results.LastOrDefault()?.DocumentId?.ToString() ?? string.Empty);
                }
                else
                {
                    var results = await query.Skip(limit - 1).FirstOrDefaultAsync();
                    pagedResults = new PagedCursorResponse<TOut>([], count, precount, pageSize, results.DocumentId?.ToString() ?? string.Empty);
                }

                sw.Stop();
                Log.Information("Paged {Type} {@page} in {Time}", typeof(T).Name, pagedResults.PageDetails(), sw);

                return pagedResults;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error Paging Documents for {type}", typeof(T).Name);
                return Error.Failure($"{typeof(T)}.PaginateDocumentsByCursorFailure", $"Error paging Documents. Error: {ex.Message}");
            }
        }

        private protected async IAsyncEnumerable<IEnumerable<TOut>> EnumerateDocuments<TOut>(FilterDefinition<T> filter, int batchSize = 10000, ProjectionDefinition<T, TOut>? projection = null)
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
        //TODO Find the key violation Excpetions and return conflict ErrorType
        public async Task<Result> Create(T document)
        {
            try
            {
                await Collection.InsertOneAsync(document);
                Log.Debug("Created {Count} {Type}", 1, typeof(T).Name);
                return Result.Success();
            }
            catch (MongoWriteException ex)
            {
                Log.Error(ex, "Error Creating {Type} {@document}", typeof(T).Name, document);
                return Error.WriteError($"{typeof(T)}.WriteError", $"Could not create {@document} due to {ex.WriteError.Category}. {ex.WriteError.Message}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error Creating {Type} {@document}", typeof(T).Name, document);
                return Error.Failure($"{typeof(T)}.CreationFailure", $"Could not create {@document}. Error: {ex.Message}");
            }
        }

        public async Task<Result> Create(T[] documents)
        {
            try
            {
                await Collection.InsertManyAsync(documents);
                Log.Debug("Created {Count} {Type}", documents.Length, typeof(T).Name);
                return Result.Success();
            }
            catch (MongoBulkWriteException<T> ex)
            {
                Log.Error(ex, "Error Creating {Type} {errorCount}/{requestCount} \r\n{@errors}", typeof(T).Name, ex.WriteErrors.Count, ex.Result.RequestCount, JsonSerializer.Serialize(ex.WriteErrors));
                return Error.WriteError($@"{typeof(T)}.WriteError", $"Could not create {ex.WriteErrors.Count} documents.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error Creating multiple {Type} {count}", typeof(T).Name, documents.Length);
                return Error.Failure($"{typeof(T)}.CreationFailure", $"Could not create {documents.Length} documents. Error: {ex.Message}");
            }
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
        #endregion

        #region Deletion
        public async Task<Result<DeleteResult>> DeleteByFilter(FilterDefinition<T> filter)
        {
            try
            {
                var result = await Collection.DeleteManyAsync(filter);
                Log.Debug("Deleted {Count} {Type}", result.DeletedCount, typeof(T).Name);
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error Deleting {Type} from filter: {filter}", typeof(T).Name, filter.ToString());
                return Error.Failure($"{typeof(T)}.DeletionFailure", $"Could not delete document. Error: {ex.Message}");
            }
        }

        public async Task<Result<DeleteResult>> DeleteAll()
        {
            var filter = Builders<T>.Filter.Empty;
            return await this.DeleteByFilter(filter);
        }

        public async Task<Result<T>> FindOneAndDelete(FilterDefinition<T> filter)
        {
            try
            {
                var result = await Collection.FindOneAndDeleteAsync(filter);
                Log.Debug("Deleted 1 {Type}", typeof(T).Name);
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error Deleting {Type} from filter: {filter}", typeof(T).Name, filter.ToString());
                return Error.Failure($"{typeof(T)}.DeletionFailure", $"Could not delete document. Error: {ex.Message}");
            }
        }

        public async Task<Result<T>> DeleteById(Tid documentId)
        {
            return await this.FindOneAndDelete(this.FilterByDocumentId(documentId));
        }

        public async Task<Result<T>> Delete(T document)
        {
            return await this.DeleteById(document.DocumentId);
        }

        [Obsolete]
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

        public async Task<Result<T>> UpdateStatus(Tid documentId, Status status, string? detail = null)
        {
            return await this.Update(documentId).AppendPipeline(c => c.AppendStatus(VersionProvider.NewStatus(status, detail))).FindAndUpdateDocument();
        }
        public async Task<Result<T>> UpdateStatus(T document, Status status, string? detail = null)
        {
            return await this.Update(document).AppendPipeline(c => c.AppendStatus(VersionProvider.NewStatus(status, detail))).FindAndUpdateDocument();
        }

        public async Task<Result<UpdateResult>> UpdateStatus(FilterDefinition<T> filter, Status status, string? detail = null)
        {
            return await this.Update(filter).AppendPipeline(c => c.AppendStatus(VersionProvider.NewStatus(status, detail))).UpdateDocuments();
        }
    }

    public class BaseServiceWithDifferences<T, Tid>(IMongoCollection<T> Collection, IVersionProvider versionProvider) : BaseServiceWithVersion<T, Tid>(Collection, versionProvider)
        where T : ICollectionEntity<Tid>, IStatusHistory, IUpdateDifferences
    {
        public async Task<Result<T>> UpdateDifferences(T document, T newDocument)
        {
            string? differences = null;
            return await this.Update(document).AppendUpdate(c => c.UpdateDifferences(document, newDocument, out differences))
                                              .AppendPipeline(c => c.AppendStatus(VersionProvider.NewStatus(Status.Updated, $"Entity Updated: '{differences}'")))
                                              .FindAndUpdateDocument();
        }
    }
}
