using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Models.Attributes;
using MMIv8_Ktype.Models.Collections;
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
        Task<List<EntityRelation>> GetCurrentEntityRelations();

        [Get("/GetPreviousEntityRelations")]
        Task<List<EntityRelation>> GetPreviousEntityRelations();

        [Post("/CreateEntityRelation")]
        Task CreateEntityRelation(int versionNumber, List<PutEntityRelationRequest> entityRelationsRequest);

        [Delete("/Debug/DeleteAll")]
        Task DeleteAll();
    }
}
