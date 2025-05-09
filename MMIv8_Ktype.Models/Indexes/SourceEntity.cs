using MMIv8_Ktype.Models.Attributes;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.DateIntersection;
using MMIv8_Ktype.Models.Status;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace MMIv8_Ktype.Models.Indexes
{
    [Serializable]
    public abstract class SourceEntity : ICollectionEntity<ObjectId>, IStatusHistory, IUpdateDifferences
    {
        [BsonId]
        [DoNotUpdateDifferences]
        public ObjectId DocumentId { get; set; } = ObjectId.GenerateNewId();
        public SourceIndex SourceIndex { get; set; }
        public int ExternalId { get; set; }

        [DoNotUpdateDifferences]
        public StatusHistory Status { get; set; }

        public abstract DateTimeRange DateRange { get; }
        public abstract string EntityHash { get; }
        public abstract string SourceEntityModelHash { get; }

        public SourceEntity(SourceIndex sourceIndex, int externalId, IVersionProvider versionProvider)
        {
            SourceIndex = sourceIndex;
            ExternalId = externalId;
            Status = new(versionProvider);
        }

        [JsonConstructor]
        public SourceEntity()
        {
        }
    }
}
