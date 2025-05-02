using MMIv8_Ktype.Models.Attributes;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.DateIntersection;
using MMIv8_Ktype.Models.Status;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MMIv8_Ktype.Models.Indexes
{
    [Serializable]
    public abstract class SourceEntity(SourceIndex sourceIndex, int externalId, IVersionProvider versionProvider) : ICollectionEntity<ObjectId>, IStatusHistory, IUpdateDifferences
    {
        [BsonId]
        [DoNotUpdateDifferences]
        public ObjectId SourceEntityID { get; set; } = ObjectId.GenerateNewId();
        public SourceIndex SourceIndex { get; set; } = sourceIndex;
        public int ExternalId { get; } = externalId;

        [DoNotUpdateDifferences]
        public StatusHistory Status { get; set; } = new(versionProvider);

        public abstract DateTimeRange DateRange { get; }
        public abstract string EntityHash { get; }
        public abstract string SourceEntityModelHash { get; }

        [BsonIgnore]
        public ObjectId DocumentId => SourceEntityID;
    }
}
