using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Attributes;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Indexes;
using MongoDB.Bson;
using MongoDB.Driver;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMIv8_Ktype.Api.Endpoints
{
    public interface ISourceEntityEndpoints<T>
    {
        [Get("")]
        Task<SerializableResult<PagedCursorResponse<T>>> GetAll(string? Cursor, int PageSize);

        [Get("/GetByExternalId/{ExternalId}")]
        Task<SerializableResult<T>> GetByExternalId(int ExternalId);

        [Get("/GetEntityId/{EntityId}")]
        Task<SerializableResult<T>> GetById(ObjectId EntityId);

        [Put("/Update")]
        Task<Result> UpdateEntity(T sourceEntity);

        [Put("/Create")]
        Task<Result> Create(T sourceEntities);

        [Put("/Debug/Bulkload")]
        Task<Result> Bulkload(T[] sourceEntities);

        [Delete("/Debug/DeleteAll")]
        Task<SerializableResult<DeleteResult>> DeleteAll();
    }

    [GroupName("SourceMMIv8Entity")]
    public interface ISourceMMIv8Endpoints : ISourceEntityEndpoints<SourceMMIv8>, IEndpoint
    {
    }

    [GroupName("SourceTecDocPCEntity")]
    public interface ISourceTecDocPCEndpoints : ISourceEntityEndpoints<SourceTecDocPC>, IEndpoint
    {
    }
}
