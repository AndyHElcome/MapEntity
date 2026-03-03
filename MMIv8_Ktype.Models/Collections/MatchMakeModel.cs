using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text;

namespace MMIv8_Ktype.Models.Collections
{

    public class MatchMakeModel : ICollectionEntity<ObjectId>, IStatusHistory
    {
        [BsonId]
        public ObjectId DocumentId { get; set; }

        [BsonIgnoreIfDefault]
        public MongoSourceEntityModel TecDocModel { get; set; }

        [BsonIgnoreIfDefault]
        public MongoSourceEntityModel MMIv8Model { get; set; }
        public StatusHistory Status { get; set; }

        [BsonElement]
        public string TextSort
        {
            get
            {
                var naturalSortParameters = new NaturalSortParameters
                {
                    StripDash = true,
                    StripSpace = true,
                    IgnoreDecimalPlaces = true
                };
                var stringBuilder = new StringBuilder()
                    .Append(NaturalSortGenerator.Generate((TecDocModel.Make ?? MMIv8Model.Make).ToLower(), naturalSortParameters)).Append('\t')
                    .Append(NaturalSortGenerator.Generate((TecDocModel.Model ?? MMIv8Model.Model).ToLower(), naturalSortParameters));

                return stringBuilder.ToString();
                    
              } 
        }




        [ Obsolete("VersionProvider Required", true) ]
        [ BsonConstructor ]

        public MatchMakeModel()
        {
            //throw new NotImplementedException("VersionProvider Required");
        }

        public MatchMakeModel(MongoSourceEntityModel tecDocModel, MongoSourceEntityModel mmiv8Model, IVersionProvider versionProvider)
        {
            DocumentId = ObjectId.GenerateNewId();
            TecDocModel = tecDocModel;
            MMIv8Model = mmiv8Model;
            Status = new(versionProvider);
        }
    }
}
