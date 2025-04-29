
using MongoDB.Bson;

namespace MMIv8_Ktype.Models.Collections
{
    public class User(string Name)
    {
        [MongoDB.Bson.Serialization.Attributes.BsonId]
        public ObjectId UserID { get; set; } = ObjectId.GenerateNewId();
        public string Name { get; set; } = Name;
    }
}
