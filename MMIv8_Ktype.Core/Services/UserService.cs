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
        public async Task<User> GetByName(string userName)
        {
            var filter = Builders<User>.Filter.Eq(e => e.Name, userName);
            return await base.GetFindFluent(filter).FirstOrDefaultAsync();
        }

        public async Task Create(string newUserName)
        {
            if (await this.GetByName(newUserName) is not null)
                return;

            await base.Create(new User(newUserName));
        }

        public async Task<User> CreateAndReturn(string newUserName)
        {
            await this.Create(newUserName);
            return await this.GetByName(newUserName);
        }

        public async Task DeleteByName(string userName)
        {
            var filter = Builders<User>.Filter.Eq(e => e.Name, userName);
            await base.DeleteByFilter(filter);
        }
    }
}
