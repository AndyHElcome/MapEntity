using MMIv8_Ktype.Models.Attributes;
using MMIv8_Ktype.Models.DateIntersection;
using MMIv8_Ktype.Models.Status;
using MongoDB.Bson;

namespace MMIv8_Ktype.Models.Indexes
{
    [Serializable]
    public abstract class SourceEntity(IVersionProvider versionProvider) : IStatusHistory, IUpdateDifferences
    {
        [MongoDB.Bson.Serialization.Attributes.BsonId]
        public ObjectId SourceEntityID { get; set; }

        public SourceIndex SourceIndex { get; set; }

        [DoNotUpdateDifferences]
        public StatusHistory Status { get; set; } = new(versionProvider);
        public abstract int ExternalId { get; }
        public abstract DateTimeRange DateRange { get; }
        public abstract string EntityHash { get; }
        public abstract string SourceEntityModelHash { get; }
    }
}
