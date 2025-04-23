using MongoDB.Driver;

namespace MMIv8_Ktype.Api.Requests
{
    public record PagedRequest(int Page = 1, int? PageSize = null) : IRequest;
    public record PagedSortFilter<T>(PagedRequest PagedRequest, FilterDefinition<T>? Filter = null, SortDefinition<T>? Sort = null) : IRequest;
}
