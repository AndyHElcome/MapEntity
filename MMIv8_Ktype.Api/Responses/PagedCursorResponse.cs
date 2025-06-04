using MMIv8_Ktype.Models;

namespace MMIv8_Ktype.Api.Responses
{
    public class PagedCursorResponse<T>(List<T> documents,
                                  int totalDocuments,
                                  int page,
                                  int pageSize,
                                  string cursor) : IResponse
    {
        public List<T> Documents { get; } = documents;

        public int TotalDocuments { get; } = totalDocuments;
        public int Page { get; } = page;
        public int PageSize { get; } = pageSize;
        public string Cursor { get; } = cursor;

        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => TotalDocuments != ((Page - 1) * PageSize) + Documents.Count;

        public override string ToString()
        {
            return $"Documents: {Documents.Count} / Total: {TotalDocuments} (Page:{Page} PageSize:{PageSize} Prev:{HasPreviousPage} Next:{HasNextPage})";
        }
        public object PageDetails()
        {
            return new { Documents = Documents.Count, TotalDocuments, Page, PageSize, HasPreviousPage, HasNextPage };
        }
    }
}
