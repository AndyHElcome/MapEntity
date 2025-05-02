using MongoDB.Bson.Serialization.Attributes;

namespace MMIv8_Ktype.Models.Collections
{
    [Serializable]
    public class MongoSourceEntityModel : ICollectionEntity<string>
    {
        [BsonId]
        public string SourceEntityModelHash { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }

        [BsonIgnoreIfNull]
        public List<string>? DetailedModels { get; set; }
        public int? Popularity { get; set; }

        [BsonIgnore]
        public string DocumentId => SourceEntityModelHash;
    }
}
