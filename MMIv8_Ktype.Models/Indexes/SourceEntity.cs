using MMIv8_Ktype.Models.Attributes;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.DateIntersection;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Text;
using System.Text.Json.Serialization;

namespace MMIv8_Ktype.Models.Indexes
{
    [Serializable]
    public abstract class SourceEntity : ICollectionEntity<ObjectId>, IStatusHistory, IUpdateDifferences, IEquatable<SourceEntity>
    {
        [BsonId]
        [DoNotUpdateDifferences]
        public ObjectId DocumentId { get; set; } = ObjectId.GenerateNewId();
        public SourceIndex SourceIndex { get; set; }
        public int ExternalId { get; set; }

        [DoNotUpdateDifferences]
        public StatusHistory Status { get; set; }

        public abstract DateOnlyRange DateRange { get; }
        public abstract string EntityHash { get; }
        public abstract string SourceEntityModelHash { get; }
        public abstract string TextSort { get; }

        public MatchRefine MatchRefine { get; set; } = new();

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

        public bool Equals(SourceEntity? other) 
            => other is not null && EntityHash == other.EntityHash && Status.Current.Status == other.Status.Current.Status;

        public override bool Equals(object? obj) 
            => Equals(obj as SourceEntity);

        public static bool operator ==(SourceEntity sourceEntity1, SourceEntity sourceEntity2) 
            => sourceEntity1 is null ? sourceEntity2 is null : sourceEntity1.Equals(sourceEntity2);

        public static bool operator !=(SourceEntity sourceEntity1, SourceEntity sourceEntity2) 
            => sourceEntity1 is null ? sourceEntity2 is not null : !sourceEntity1.Equals(sourceEntity2);
    }
}
