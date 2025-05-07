

namespace MMIv8_Ktype.Api.Responses
{
    public class PagedResponse<T>(List<T> items,
                                  int totalItems,
                                  int page,
                                  int pageSize) : IResponse
    {
        public List<T> Items { get; } = items;

        public int TotalItems { get; } = totalItems;
        public int Page { get; } = page;
        public int PageSize { get; } = pageSize;

        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => TotalItems != ((Page - 1) * PageSize) + Items.Count;
    }
}
