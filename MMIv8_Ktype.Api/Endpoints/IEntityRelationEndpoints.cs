using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Attributes;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Util;
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
    [GroupName("EntityRelation")]
    public interface IEntityRelationEndpoints : IBaseEndpoint<EntityRelation, ObjectId, FilterQuery<EntityRelation>, SortQuery<EntityRelation>>, IEndpoint
    {
        [Get("/GetCurrentEntityRelations")]
        Task<SerializableResult<PagedCursorResponse<EntityRelation>>> GetCurrentEntityRelations([AsParameters] PagedCursorRequest<ObjectId> pagedRequest);

        [Get("/GetPreviousEntityRelations")]
        Task<SerializableResult<PagedCursorResponse<EntityRelation>>> GetPreviousEntityRelations([AsParameters] PagedCursorRequest<ObjectId> pagedRequest);

        [Post("/CreateEntityRelation")]
        Task<Result> CreateEntityRelation(int versionNumber, List<PutEntityRelationRequest> entityRelationsRequest);

        [Delete("/Debug/DeleteAll")]
        Task<Result> DeleteAll();
    }
}
