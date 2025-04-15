using CsvHelper.Configuration;
using MMIv8_Ktype.Models.Status;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Globalization;

namespace MMIv8_Ktype.Models.Collections
{

    public class MatchMakeModel : IStatusHistory
    {
        [MongoDB.Bson.Serialization.Attributes.BsonId]
        [CsvHelper.Configuration.Attributes.Ignore]
        public ObjectId MatchID { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonIgnoreIfDefault]
        public MongoSourceEntityModel TecDocModel { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonIgnoreIfDefault]
        public MongoSourceEntityModel MMIv8Model { get; set; }
        public StatusHistory Status { get; set; }

        [Obsolete("VersionProvider Required", true)]
        public MatchMakeModel()
        {
            throw new NotImplementedException("VersionProvider Required");
        }

        public MatchMakeModel(MongoSourceEntityModel tecDocModel, MongoSourceEntityModel mmiv8Model, IVersionProvider versionProvider)
        {
            MatchID = ObjectId.GenerateNewId();
            TecDocModel = tecDocModel;
            MMIv8Model = mmiv8Model;
            Status = new(versionProvider);
        }
    }

    public sealed class MatchMakeModelMap : ClassMap<MatchMakeModel>
    {
        public MatchMakeModelMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.MatchID).TypeConverter<ObjectIdConverter>();
            References<MongoSourceEntityModelMap>(m => m.TecDocModel).Prefix("TD_");
            References<MongoSourceEntityModelMap>(m => m.MMIv8Model).Prefix("MMI_");
            //Map(m => m.StatusHistory).TypeConverter<StatusHistoryConverter>();

            References<StatusHistoryMap>(m => m.Status);
        }
    }

    public class ImportMatchMakeModel
    {
        public string TD_SourceEntityModelHash { get; set; } = string.Empty;
        public string MMI_SourceEntityModelHash { get; set; } = string.Empty;
        public bool DeleteMatch { get; set; } = false;
    }

    public sealed class ImportMatchMakeModelMap : ClassMap<ImportMatchMakeModel>
    {
        public ImportMatchMakeModelMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
        }
    }
}
