using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace MMIv8_Ktype.Api.Requests
{
    //public record PagedRequest(int Page = 1, int? PageSize = null) : IRequest;
    public class PagedRequest : IRequest
    {
        public int Page { get; set; } = 1;
        public int? PageSize { get; set; } = null;
    }
    public record struct PagedRequest1([FromQuery] int Page = 1,[FromQuery] int? PageSize = null);

    public record PagedSortFilter<T>(PagedRequest PagedRequest, FilterDefinition<T>? Filter = null, SortDefinition<T>? Sort = null) : IRequest;
}
