using MMIv8_Ktype.Models.Attributes;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Indexes;
using MongoDB.Bson;
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
        Task<List<T>> GetAll();

        [Get("GetByExternalId/{ExternalId}")]
        Task<T?> GetByExternalId(int ExternalId);

        [Get("GetEntityId/{EntityId}")]
        Task<T?> GetById(ObjectId EntityId);

        [Put("")]
        Task UpdateEntity(T sourceEntity);
    }

    [GroupName("SourceMMIv8Entity")]
    public interface ISourceMMIv8Endpoints : ISourceEntityEndpoints<MongoSourceMMIv8>, IEndpoint
    {
    }

    [GroupName("SourceTecDocPCEntity")]
    public interface ISourceTecDocPCEndpoints : ISourceEntityEndpoints<MongoSourceTecDocPC>, IEndpoint
    {
    }
}
