

namespace MMIv8_Ktype.Api.Responses
{
    public class PagedResponse<T>(List<T> items,
                                  int totalItems,
                                  int page,
                                  int pageSize) : IResponse
    {
        public List<T> Items = items;

        public int TotalItems = totalItems;
        public int Page = page;
        public int PageSize = pageSize;

        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => TotalItems != ((Page - 1) * PageSize) + Items.Count;
    }
}
