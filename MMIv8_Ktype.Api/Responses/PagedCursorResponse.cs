using MMIv8_Ktype.Models;

namespace MMIv8_Ktype.Api.Responses
{
    public class PagedCursorResponse<T>(List<T> documents,
                                  int totalDocuments,
                                  int coveredDocuments,
                                  int pageSize,
                                  string cursor) : IResponse
    {
        public List<T> Documents { get; } = documents;

        public int TotalDocuments { get; } = totalDocuments;
        public int CoveredDocuments { get; } = coveredDocuments;
        public int RemmainingDocuments => TotalDocuments - (CoveredDocuments + Documents.Count);

        public int PageSize { get; } = pageSize;
        public string Cursor { get; } = cursor;

        public bool HasPreviousPage => CoveredDocuments > 0;
        public bool HasNextPage => RemmainingDocuments > 0;

        public override string ToString()
        {
            return $"Documents: {CoveredDocuments}(+{Documents.Count}) / Total: {TotalDocuments} (PageSize:{PageSize} Next:{HasNextPage})";
        }
        public object PageDetails()
        {
            return new { Documents = Documents.Count, TotalDocuments, CoveredDocuments, RemmainingDocuments, PageSize, HasPreviousPage, HasNextPage };
        }
    }
}
