using MMIv8_Ktype.Models.Status;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MMIv8_Ktype.Models.Collections
{

    public class MatchMakeModel : ICollectionEntity<ObjectId>, IStatusHistory
    {
        [BsonId]
        public ObjectId MatchID { get; set; }

        [BsonIgnoreIfDefault]
        public MongoSourceEntityModel TecDocModel { get; set; }

        [BsonIgnoreIfDefault]
        public MongoSourceEntityModel MMIv8Model { get; set; }
        public StatusHistory Status { get; set; }

        [BsonIgnore]
        public ObjectId DocumentId => MatchID;

        [Obsolete("VersionProvider Required", true)]
        [BsonConstructor]
        public MatchMakeModel()
        {
            //throw new NotImplementedException("VersionProvider Required");
        }

        public MatchMakeModel(MongoSourceEntityModel tecDocModel, MongoSourceEntityModel mmiv8Model, IVersionProvider versionProvider)
        {
            MatchID = ObjectId.GenerateNewId();
            TecDocModel = tecDocModel;
            MMIv8Model = mmiv8Model;
            Status = new(versionProvider);
        }
    }
}
