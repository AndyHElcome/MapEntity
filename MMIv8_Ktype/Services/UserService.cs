using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Models.Collections;
using MongoDB.Driver;

namespace MMIv8_Ktype.Core.Services
{
    public class UserService(MongoDBContext MMIv8_Ktype, 
                             MongoBaseContext BaseContext)
    {
        public async Task<IAsyncCursor<User>> GetAll()
        {
            return await BaseContext.GetCursor(MMIv8_Ktype.Collections.User);
        }

        public async Task<User?> GetByName(string userName)
        {
            var filter = Builders<User>.Filter.Eq(e => e.Name, userName);
            return await BaseContext.GetSingleDocument(MMIv8_Ktype.Collections.User, filter);
        }

        public async Task Create(string newUser)
        {
            var filter = Builders<User>.Filter.Eq(e => e.Name, newUser);
            var user = await BaseContext.GetSingleDocument(MMIv8_Ktype.Collections.User, filter);

            if (user is not null)
                throw new Exception($"User '" + newUser + "' already exists");

            await BaseContext.Create(MMIv8_Ktype.Collections.User, new User(newUser));
        }

        public async Task Delete(string userName)
        {
            var filter = Builders<User>.Filter.Eq(e => e.Name, userName);
            await BaseContext.Delete(MMIv8_Ktype.Collections.User, filter);
        }
    }
}
