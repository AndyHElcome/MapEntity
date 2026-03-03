using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Api.Requests;
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
        where T : SourceEntity
    {
        [Get("/GetByExternalId/{ExternalId}")]
        Task<SerializableResult<T>> GetByExternalId(int ExternalId);

        [Get("/GetMakes")]
        Task<SerializableResult<List<string>>> GetMakes([FromQuery] string[]? makes, [FromQuery] string[]? models);

        [Get("/GetModels")]
        Task<SerializableResult<List<string>>> GetModels([FromQuery] string[]? makes, [FromQuery] string[]? models);

        [Get("/GetByMakeModel")]
        Task<SerializableResult<MongoSourceEntityModel>> GetByMakeModel([FromQuery] string make, [FromQuery] string model);

        [Put("/Update")]
        Task<Result> UpdateEntity(T sourceEntity);

        [Put("/Create")]
        Task<Result> Create(T sourceEntities);

        [Put("/Debug/Bulkload")]
        Task<Result> Bulkload(T[] sourceEntities);

        [Post("/Debug/RegenerateTextSort")]
        Task RegenerateTextSort();

        [Delete("/Debug/DeleteAll")]
        Task<Result> DeleteAll();
    }

    [GroupName("SourceMMIv8Entity")]
    public interface ISourceMMIv8Endpoints : ISourceEntityEndpoints<SourceMMIv8>, IBaseEndpoint<SourceMMIv8, ObjectId, SourceMMIv8FilterRequest, SourceEntitySortRequest<SourceMMIv8>>, IEndpoint
    {
    }

    [GroupName("SourceTecDocPCEntity")]
    public interface ISourceTecDocPCEndpoints : ISourceEntityEndpoints<SourceTecDocPC>, IBaseEndpoint<SourceTecDocPC, ObjectId, SourceTecDocPCFilterRequest, SourceEntitySortRequest<SourceTecDocPC>>, IEndpoint
    {
    }
}
