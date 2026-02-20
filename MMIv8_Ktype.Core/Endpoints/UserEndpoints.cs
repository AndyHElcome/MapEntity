using Microsoft.AspNetCore.Http.HttpResults;
using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Core.Services;
using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;
using Version = MMIv8_Ktype.Models.Collections.Version;

namespace MMIv8_Ktype.Core.Endpoints
{
    public class UserEndpoints(UserService userService, MappingService mappingService) : BaseEndpoints<User, ObjectId, FilterQuery<User>, SortQuery<User>>(userService), IUserEndpoints
    {

        public async Task<SerializableResult<User>> GetByUserName(string UserName)
        {
            return await userService.GetByName(UserName);
        }

        public async Task<SerializableResult<User>> Create(string UserName)
        {
            return await userService.Create(UserName);
        }

        public async Task<Result> Delete(string UserName)
        {
            return await mappingService.DeleteUser(UserName);
        }

        public async Task<Result> DeleteAll()
        {
            return await userService.DeleteAll();
        }
    }
}
