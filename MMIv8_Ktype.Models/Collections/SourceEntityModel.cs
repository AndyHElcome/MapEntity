using CsvHelper.Configuration;
using MongoDB.Bson.Serialization.Attributes;
using System.Globalization;

namespace MMIv8_Ktype.Models.Collections
{
    [Serializable]
    public class MongoSourceEntityModel
    {
        [BsonId]
        public string SourceEntityModelHash { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }

        [BsonIgnoreIfNull]
        public List<string>? DetailedModels { get; set; }
        public int? Popularity { get; set; }
    }

    public sealed class MongoSourceEntityModelMap : ClassMap<MongoSourceEntityModel>
    {
        public MongoSourceEntityModelMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
        }
    }
}
