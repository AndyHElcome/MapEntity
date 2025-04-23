using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MMIv8_Ktype.Models.Collections
{
    public class Version
    {
        [BsonId]
        public ObjectId VersionID { get; set; } = ObjectId.GenerateNewId();
        public int VersionNumber { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string TecDocEntityVersion { get; set; }
        public string MMIv8EntityVersion { get; set; }
        public User User { get; set; }
    }
}
