using MMIv8_Ktype.Models.Outputs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MMIv8_Ktype.Models.Collections
{
    public class Version(int versionNumber, string tecdocEntityVersion, string mmiv8EntityVersion, User user) : ICollectionEntity<ObjectId>
    {
        [BsonId]
        public ObjectId DocumentId { get; set; } = ObjectId.GenerateNewId();
        public int VersionNumber { get; set; } = versionNumber;
        public DateTime Date { get; set; } = DateTime.Now;
        public string TecDocEntityVersion { get; set; } = tecdocEntityVersion;
        public string MMIv8EntityVersion { get; set; } = mmiv8EntityVersion;
        public User User { get; set; } = user;
    }
}
