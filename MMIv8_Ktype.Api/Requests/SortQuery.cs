// minimal endpoint https://youtu.be/gsAuFIhXz3g?si=MfaGxzKFgLlgWIbR
// reflection endpoint mapping https://youtu.be/CkGFV5bekbY?si=GkVIYuPIObrZDMu1
namespace MMIv8_Ktype.Api.Requests
{
    public class SortQuery<T>
    {
        public MongoDB.Driver.SortDefinition<T>? GetSort() => null;
    }
}
