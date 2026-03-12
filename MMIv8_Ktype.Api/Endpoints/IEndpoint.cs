using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Status;
using MongoDB.Driver;
using Refit;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Cryptography;

namespace MMIv8_Ktype.Api.Endpoints
{
    public interface IEndpoint //TODO Maybe move to custom attribute
    {
    }

    public interface IBaseEndpoint<T, Tid, TFilter, TSort>
        where T : ICollectionEntity<Tid>
        where TFilter : FilterQuery<T>, new()
        where TSort : SortQuery<T>, new()
    {
        [Get("/{DocumentId}")]
        Task<SerializableResult<T>> GetById(Tid DocumentId);

        [Post("/GetByCursor")]
        Task<SerializableResult<PagedCursorResponse<T>>> GetByCursor([FromBody] BasicGetByCursorRequest<T, Tid, TFilter> Request);

        [Post("/GetByPage")]
        Task<SerializableResult<PagedResponse<T>>> GetByPage([FromBody] BasicGetByPageRequest<T, TFilter, TSort> Request);
    }

    public interface IVersionEndpoint<T, Tid, TFilter>
        where T : ICollectionEntity<Tid>
        where TFilter : FilterQuery<T>, new()
    {
        [Patch("/UpdateStatus/{DocumentId}")]
        Task<SerializableResult<T>> UpdateStatusById(Tid DocumentId, UpdateStatusRequest<T> Request);

        //[Patch("/UpdateStatus")]
        //Task<SerializableResult<T>> UpdateStatus(T Document, [FromQuery] Status NewStatus, string? Detail = null);

        [Post("/UpdateStatuses")]
        Task<SerializableResult<UpdateResultDTO>> UpdateStatuses([FromBody] UpdateStatusesRequest<T, TFilter> Request);
    }
}
