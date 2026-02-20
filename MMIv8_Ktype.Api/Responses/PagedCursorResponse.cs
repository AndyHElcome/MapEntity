using MMIv8_Ktype.Models;

namespace MMIv8_Ktype.Api.Responses
{
    public class PagedCursorResponse<T>(List<T> documents,
                                  int totalDocuments,
                                  int coveredDocuments,
                                  int pageSize,
                                  string cursor) : BasePagedResponse
    {
        public List<T> Documents { get; } = documents;

        public override int Count { get; } = documents.Count;
        public override int PageSize { get; } = pageSize;
        public override int TotalDocuments { get; } = totalDocuments;
        public override int CoveredDocuments { get; } = coveredDocuments;
        public override int CurrentPage => (CoveredDocuments / PageSize) + 1;

        public string Cursor { get; } = cursor;
    }
}
