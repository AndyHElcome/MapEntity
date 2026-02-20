using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Attributes;
using MMIv8_Ktype.Models.Collections;
using MongoDB.Bson;
using MongoDB.Driver;
using Refit;
using System.Threading.Tasks;
using Version = MMIv8_Ktype.Models.Collections.Version;

namespace MMIv8_Ktype.Api.Endpoints
{
    [GroupName("User")]
    public interface IUserEndpoints : IBaseEndpoint<User, ObjectId, FilterQuery<User>, SortQuery<User>>, IEndpoint
    {
        [Get("/{UserName}")]
        Task<SerializableResult<User>> GetByUserName(string UserName);

        [Put("/Create")]
        Task<SerializableResult<User>> Create(string UserName);

        [Delete("/{UserName}")]
        Task<Result> Delete(string UserName);

        [Delete("/Debug/DeleteAll")]
        Task<Result> DeleteAll();
    }
}
