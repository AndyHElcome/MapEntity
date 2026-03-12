// minimal endpoint https://youtu.be/gsAuFIhXz3g?si=MfaGxzKFgLlgWIbR
// reflection endpoint mapping https://youtu.be/CkGFV5bekbY?si=GkVIYuPIObrZDMu1
using MMIv8_Ktype.Models.Status;
using System.Security.Cryptography;

namespace MMIv8_Ktype.Api.Requests
{
    public interface IRequest { }


    public class BasicGetByCursorRequest<T, Tid, TFilter>(PagedCursorRequest<Tid>? pagedRequest, TFilter? filterQuery)
        where TFilter : FilterQuery<T>, new()
    {
        public PagedCursorRequest<Tid> PagedRequest { get; set; } = pagedRequest ?? new();
        public TFilter FilterQuery { get; set; } = filterQuery ?? new();
    }

    public class BasicGetByPageRequest<T, TFilter, TSort>(PagedRequest? pagedRequest, TFilter? filterQuery, TSort? sortQuery)
        where TFilter : FilterQuery<T>, new()
        where TSort : SortQuery<T>, new()
    {
        public PagedRequest PagedRequest { get; set; } = pagedRequest ?? new();
        public TFilter FilterQuery { get; set; } = filterQuery ?? new();
        public TSort SortQuery { get; set; } = sortQuery ?? new();
    }

    public class UpdateStatusRequest<T>(Status newStatus, string? detail) 
    {
        public Status NewStatus { get; set; } = newStatus;
        public string? Detail { get; set; } = detail;
    }

    public class UpdateStatusesRequest<T, TFilter>(Status newStatus, string? detail, TFilter filterQuery)
        where TFilter : FilterQuery<T>, new()
    {
        public Status NewStatus { get; set; } = newStatus;
        public string? Detail { get; set; } = detail;
        public TFilter FilterQuery { get; set; } = filterQuery ?? new();
    }
}
