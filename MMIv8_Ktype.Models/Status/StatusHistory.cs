
namespace MMIv8_Ktype.Models.Status
{

    public class StatusHistory
    {
        public StatusHistory(IVersionProvider versionProvider)
        {
            History = new([ versionProvider.NewStatus(Status.Created) ]);
        }

        [Obsolete("Version is now required")]
        public StatusHistory()
        {
        }

        [MongoDB.Bson.Serialization.Attributes.BsonElement]
        public StatusChange Current => History.Peek();
        public Stack<StatusChange> History { get; set; }
    }
}
