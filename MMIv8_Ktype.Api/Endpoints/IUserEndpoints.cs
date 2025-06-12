using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Models.Attributes;
using MongoDB.Bson;
using Version = MMIv8_Ktype.Models.Collections.Version;
using Refit;
using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Models.Collections;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Threading.Tasks;
using MongoDB.Driver;
using MMIv8_Ktype.Models;

namespace MMIv8_Ktype.Api.Endpoints
{
    [GroupName("User")]
    public interface IUserEndpoints : IEndpoint
    {
        [Get("")]
        Task<SerializableResult<List<User>>> GetAll();

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
