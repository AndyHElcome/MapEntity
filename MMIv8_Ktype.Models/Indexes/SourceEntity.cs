using MMIv8_Ktype.Models.Attributes;
using MMIv8_Ktype.Models.Status;
using MongoDB.Bson;

namespace MMIv8_Ktype.Models.Indexes
{
    [Serializable]
    public class SourceEntity(IVersionProvider versionProvider) : IStatusHistory, IUpdateDifferences
    {
        [MongoDB.Bson.Serialization.Attributes.BsonId]
        public ObjectId SourceEntityID { get; set; }

        public SourceIndex SourceIndex { get; set; }

        [DoNotUpdateDifferences]
        public StatusHistory Status { get; set; } = new(versionProvider);
    }
}
