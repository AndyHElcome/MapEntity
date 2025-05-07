using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMIv8_Ktype.Models.Collections
{
    [Serializable]
    public class EntityRelation(ObjectId versionID, int mmi_V8_Key, int ktypNr, string? comment, int versionNumber) : ICollectionEntity<ObjectId>
    {
        [BsonId]
        public ObjectId DocumentId { get; set; } = ObjectId.GenerateNewId();
        public ObjectId VersionID { get; set; } = versionID;
        [BsonElement]
        public string RelationKey => $"{MMI_V8_Key}-{KTypNr}";
        public int MMI_V8_Key { get; set; } = mmi_V8_Key;
        public int KTypNr { get; set; } = ktypNr;
        public string? Comment { get; set; } = comment;
        public int VersionNumber { get; set; } = versionNumber;
    }
}
