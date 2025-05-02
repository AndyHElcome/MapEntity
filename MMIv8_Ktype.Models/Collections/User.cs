
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Security.Cryptography;

namespace MMIv8_Ktype.Models.Collections
{
    public class User(string Name) : ICollectionEntity<ObjectId>
    {
        [MongoDB.Bson.Serialization.Attributes.BsonId]
        public ObjectId UserID { get; set; } = ObjectId.GenerateNewId();
        public string Name { get; set; } = Name;

        [BsonIgnore]
        public ObjectId DocumentId => UserID;
    }
}
