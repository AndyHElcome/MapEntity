
using System.Text.Json.Serialization;

namespace MMIv8_Ktype.Models.Status
{
    [Serializable]
    public class StatusHistory
    {
        public StatusHistory(IVersionProvider versionProvider)
        {
            History = new([ versionProvider.NewStatus(Status.Created) ]);
        }

        [JsonConstructor]
        [Obsolete("Version is now required")]
        public StatusHistory()
        {
        }

        [MongoDB.Bson.Serialization.Attributes.BsonElement]
        public StatusChange Current => History.Peek();
        public Stack<StatusChange> History { get; set; }
    }
}
