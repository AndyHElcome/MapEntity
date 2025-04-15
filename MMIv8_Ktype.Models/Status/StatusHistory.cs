using CsvHelper.Configuration;

namespace MMIv8_Ktype.Models.Status
{

    public class StatusHistory
    {
        public StatusHistory(IVersionProvider versionProvider)
        {
            History = new([ versionProvider.NewStatus(Status.Created) ]);
        }

        [Obsolete("Version is now required", true)]
        public StatusHistory()
        {
        }

        [MongoDB.Bson.Serialization.Attributes.BsonElement]
        public StatusChange Current => History.Peek();
        public Stack<StatusChange> History { get; set; }


    }

    public sealed class StatusHistoryMap : ClassMap<StatusHistory>
    {
        public StatusHistoryMap()
        {
            //References<StatusChangeMap>(m => m.Current);
            Map(m => m.Current.Status);
            Map(m => m.Current.Detail);
            Map(m => m.History).Ignore();
        }
    }
}
