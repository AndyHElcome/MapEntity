using MMIv8_Ktype.Core.Services;
using MMIv8_Ktype.Api.Endpoints;
using MongoDB.Driver;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Core.Services.Mapping;
using Version = MMIv8_Ktype.Models.Collections.Version;
using MongoDB.Bson;
using MMIv8_Ktype.Models.Collections;
using Serilog;
using MMIv8_Ktype.Core.Services.Match;
using Microsoft.AspNetCore.Http.HttpResults;
using MMIv8_Ktype.Models;

namespace MMIv8_Ktype.Core.Endpoints
{
    public class UserEndpoints(UserService userService, MappingService mappingService) : IUserEndpoints
    {
        public async Task<SerializableResult<List<User>>> GetAll()
        {
            return Result.Success(await userService.GetFindFluent().ToListAsync());
        }

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
