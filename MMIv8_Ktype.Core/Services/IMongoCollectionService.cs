using MongoDB.Driver;

namespace MMIv8_Ktype.Core.Services
{
    public interface IMongoCollectionService<T>
    {
        public IMongoCollection<T> Collection { get; }
    }

}
