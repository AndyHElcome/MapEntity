

using MMIv8_Ktype.Models.Util;
using System.Reflection.Metadata;
using System.Xml;

namespace MMIv8_Ktype.Api.Responses
{
    public class PagedResponse<T>(List<T> documents,
                                  int totalDocuments,
                                  int page,
                                  int pageSize) : IResponse
    {
        public List<T> Documents { get; } = documents;

        public int TotalDocuments { get; } = totalDocuments;
        public int Page { get; } = page;
        public int PageSize { get; } = pageSize;

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
