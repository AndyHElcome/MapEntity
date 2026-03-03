using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Models;
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
    {
        [Get("/{DocumentId}")]
        Task<SerializableResult<T>> GetById(Tid DocumentId);

        [Get("/GetByCursor")]
        Task<SerializableResult<PagedCursorResponse<T>>> GetByCursor([AsParameters] PagedCursorRequest<Tid> PagedRequest, [AsParameters] TFilter Filter);

        [Get("/GetByPage")]
        Task<SerializableResult<PagedResponse<T>>> GetByPage([AsParameters] PagedRequest PagedRequest, [AsParameters] TFilter Filter, [AsParameters] TSort Sort);
    }

    public interface IVersionEndpoint<T, Tid, TFilter>
    {
        [Patch("/UpdateStatus/{DocumentId}")]
        Task<SerializableResult<T>> UpdateStatusById(Tid DocumentId, Status NewStatus, string? Detail = null);

        [Patch("/UpdateStatus")]
        Task<SerializableResult<T>> UpdateStatus(T Document, [FromQuery] Status NewStatus, string? Detail = null);

        [Post("/UpdateStatuses")]
        Task<SerializableResult<UpdateResultDTO>> UpdateStatuses([AsParameters] TFilter Filter, [FromQuery] Status NewStatus, string? Detail = null);
    }
}
