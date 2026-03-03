// minimal endpoint https://youtu.be/gsAuFIhXz3g?si=MfaGxzKFgLlgWIbR
// reflection endpoint mapping https://youtu.be/CkGFV5bekbY?si=GkVIYuPIObrZDMu1
using MongoDB.Driver;

namespace MMIv8_Ktype.Api.Requests
{
    public class FilterQuery<T>
    {
        public virtual MongoDB.Driver.FilterDefinition<T>? GetFilter() => Builders<T>.Filter.Empty;
    }
}
