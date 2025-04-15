using MMIv8_Ktype.Models.Status;
using MongoDB.Bson;

namespace MMIv8_Ktype.Models.Indexes
{
    [Serializable]
    public class SourceEntity(IVersionProvider versionProvider) : IStatusHistory, IUpdateDifferences
    {
        [MongoDB.Bson.Serialization.Attributes.BsonId]
        [CsvHelper.Configuration.Attributes.Ignore]
        public ObjectId SourceEntityID { get; set; }

        [CsvHelper.Configuration.Attributes.Ignore]
        public SourceIndex SourceIndex { get; set; }

        [DoNotUpdateDifferences]
        [CsvHelper.Configuration.Attributes.Ignore]
        public StatusHistory Status { get; set; } = new(versionProvider);
    }
}
