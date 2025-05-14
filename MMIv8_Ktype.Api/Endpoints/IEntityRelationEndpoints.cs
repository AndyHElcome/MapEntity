using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Models.Attributes;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Util;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMIv8_Ktype.Api.Endpoints
{
    [GroupName("EntityRelation")]
    public interface IEntityRelationEndpoints : IEndpoint
    {
        [Get("/GetCurrentEntityRelations")]
        Task<PagedResponse<EntityRelation>> GetCurrentEntityRelations([FromQuery] int Page, [FromQuery] int PageSize);

        [Get("/GetPreviousEntityRelations")]
        Task<PagedResponse<EntityRelation>> GetPreviousEntityRelations([FromQuery] int Page, [FromQuery] int PageSize);

        [Post("/CreateEntityRelation")]
        Task CreateEntityRelation(int versionNumber, List<PutEntityRelationRequest> entityRelationsRequest);

        [Delete("/Debug/DeleteAll")]
        Task DeleteAll();
    }
}
