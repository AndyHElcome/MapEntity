using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace MMIv8_Ktype.Core.Services
{
    public class UserService(MongoDBContext MMIv8_Ktype) : BaseService<User, ObjectId>(MMIv8_Ktype.Collections.User)
    {
        public async Task<Result<User>> GetByName(string userName)
        {
            try
            {
                var filter = Builders<User>.Filter.Eq(e => e.Name, userName);
                var user = await base.GetFindFluent(filter).FirstOrDefaultAsync();

                return user is not null ? user : Error.NotFound("User.NotFoundByUserName", $"No User exists with UserName {userName}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving User with UserName {@userName}", userName);
                return Error.Failure($"User.GetCurrentVersionFailure", $"Error getting User with UserName {userName}. Error: {ex.Message}");
            }
        }

        public async Task<Result<User>> Create(string newUserName)
        {
            //if (await this.GetByName(newUserName) is not null)
            //    return Error.Conflict("User.UserNameConflict", $"User already exists with user name {newUserName}");

            User user = new(newUserName);
            var userResult = await base.Create(user);

            return userResult.IsSuccess? user : Result.Failure<User>(userResult.Error!);
        }

        public async Task<Result<DeleteResult>> DeleteByName(string userName)
        {
            var filter = Builders<User>.Filter.Eq(e => e.Name, userName);
            return await base.DeleteByFilter(filter);
        }
    }
}
