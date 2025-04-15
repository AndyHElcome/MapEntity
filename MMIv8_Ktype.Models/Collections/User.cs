

using CsvHelper.Configuration;
using MongoDB.Bson;

namespace MMIv8_Ktype.Models.Collections
{
    public class User(string Name)
    {
        [MongoDB.Bson.Serialization.Attributes.BsonId]
        [CsvHelper.Configuration.Attributes.Ignore]
        public ObjectId UserID { get; set; } = ObjectId.GenerateNewId();
        public string Name { get; set; } = Name;
    }

    public sealed class UserMap : ClassMap<User>
    {
        public UserMap()
        {
            Map(m => m.UserID).Ignore();
            Map(m => m.Name);
        }
    }
}
