using MMIv8_Ktype.Models;
using System.Text.Json.Serialization;

namespace MMIv8_Ktype.Api.Responses
{
    public abstract class BasePagedResponse() : IResponse
    {
        abstract public int Count { get; }
        abstract public int PageSize { get; }
        abstract public int TotalDocuments { get; }
        abstract public int CoveredDocuments { get; }
        abstract public int CurrentPage { get; }

        public int RemmainingDocuments => TotalDocuments - (CoveredDocuments + Count);
        public bool HasPreviousPage => CoveredDocuments > 0;      
        public bool HasNextPage => RemmainingDocuments > 0;
        public int TotalPages => (TotalDocuments / PageSize) + (TotalDocuments % PageSize > 0 ? 1 : 0);

        public override string ToString()
        {
            return $"Documents: {CoveredDocuments}(+{Count}) / Total: {TotalDocuments} (PageSize:{PageSize} Next:{HasNextPage})";
        }
        public object PageDetails()
        {
            return new { Documents = Count, TotalDocuments, CurrentPage, PageSize, HasPreviousPage, HasNextPage };
        }
    }
}