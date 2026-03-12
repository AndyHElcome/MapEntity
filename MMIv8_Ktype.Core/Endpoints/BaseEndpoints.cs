using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Core.Services;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Status;

namespace MMIv8_Ktype.Core.Endpoints
{
    public class BaseEndpoints<T, Tid, TFilter, TSort>(BaseService<T, Tid> BaseService) : IBaseEndpoint<T, Tid, TFilter, TSort>
            where T : ICollectionEntity<Tid>
            where TFilter : FilterQuery<T>, new()
            where TSort : SortQuery<T>, new()
    {
        public async Task<SerializableResult<T>> GetById(Tid DocumentId)
        {
            return await BaseService.GetById(DocumentId);
        }

        public async Task<SerializableResult<PagedCursorResponse<T>>> GetByCursor([FromBody] BasicGetByCursorRequest<T, Tid, TFilter> Request)
        {

            return await BaseService.PaginateDocumentsByCursor<T, Tid>(filter: Request.FilterQuery.GetFilter(), cursor: Request.PagedRequest.GetCursor(), pageSize: Request.PagedRequest.PageSize ?? 0);
        }

        public async Task<SerializableResult<PagedResponse<T>>> GetByPage([FromBody] BasicGetByPageRequest<T, TFilter, TSort> Request)
        {
            return await BaseService.PaginateDocuments<T>(filter: Request.FilterQuery.GetFilter(), sort: Request.SortQuery.GetSort(), page: Request.PagedRequest.Page ?? 1, pageSize: Request.PagedRequest.PageSize ?? 0);
        }

    }

    public class BaseEndpointsWithVersion<T, Tid, TFilter, TSort>(BaseServiceWithVersion<T, Tid> BaseService) : BaseEndpoints<T, Tid, TFilter, TSort>(BaseService), IVersionEndpoint<T, Tid, TFilter>
            where T : ICollectionEntity<Tid>, IStatusHistory
            where TFilter : FilterQuery<T>, new()
            where TSort : SortQuery<T>, new()
    {
        public async Task<SerializableResult<T>> UpdateStatusById(Tid DocumentId, UpdateStatusRequest<T> Request)
        {
            return await BaseService.UpdateStatus(DocumentId, Request.NewStatus, Request.Detail);
        }

        //public async Task<SerializableResult<T>> UpdateStatus(T Document, [FromQuery] Status NewStatus, string? Detail = null)
        //{
        //    return await BaseService.UpdateStatus(Document, NewStatus, Detail);
        //}

        public async Task<SerializableResult<UpdateResultDTO>> UpdateStatuses([FromBody] UpdateStatusesRequest<T, TFilter> Request)
        {
            var filter = Request.FilterQuery.GetFilter();

            if (filter is null)
                return Error.Validation($"{nameof(T)}.BaseEndpoint.UpdateStatus", "Filter cannot be null");
            else
                return await BaseService.UpdateStatus(filter, Request.NewStatus, Request.Detail);
        }
    }
}
