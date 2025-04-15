using MongoDB.Bson;
using MongoDB.Driver;

namespace MMIv8_Ktype.Core.Services.Source
{
    public interface ISourceEntityService<T> : IMongoCollectionService<T>
    {
        Task<IAsyncCursor<T>> GetAll(int? batchSize = null);
        Task<T?> GetById(ObjectId SourceEntityID);
        Task<T?> GetByExternalId(int ExternalID);
        Task<IAsyncCursor<T>> GetByModelId(string SourceEntityModelHash, int? batchSize = null);
        CombinationPipeline<T> UpdateSourceEntity(T entity);
        Task Create(T model, bool validate = true);
        Task CreateBulk(List<T> model);
        Task<DeleteResult> DeleteAll(FilterDefinition<T>? filter = null);
        Task<DeleteResult> Delete(ObjectId id);
        Task<DeleteResult> DeleteByExternalId(int ExternalID);
    }
}
