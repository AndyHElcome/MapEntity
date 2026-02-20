

using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Util;
using System.Reflection.Metadata;
using System.Text.Json.Serialization;
using System.Xml;

namespace MMIv8_Ktype.Api.Responses
{
    [Serializable]
    public class PagedResponse<T>(List<T> documents,
                                  int totalDocuments,
                                  int currentPage,
                                  int pageSize) : BasePagedResponse
    {
        public List<T> Documents { get; } = documents;

        public override int PageSize { get; } = pageSize;
        public override int Count { get; } = documents.Count;
        public override int TotalDocuments { get; } = totalDocuments;

        public override int CoveredDocuments => (CurrentPage - 1) * PageSize;
        public override int CurrentPage { get; } = currentPage;
    }
}
