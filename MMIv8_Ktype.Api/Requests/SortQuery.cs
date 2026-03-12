// minimal endpoint https://youtu.be/gsAuFIhXz3g?si=MfaGxzKFgLlgWIbR
// reflection endpoint mapping https://youtu.be/CkGFV5bekbY?si=GkVIYuPIObrZDMu1
using MMIv8_Ktype.Models;
using MongoDB.Driver;

namespace MMIv8_Ktype.Api.Requests
{
    public class SortQuery<T>
    {
        public string GetCacheKey() => GlobalHelpers.GenerateKey(this);
        public virtual SortDefinition<T> GetSort() => Builders<T>.Sort.Combine();
    }
}
