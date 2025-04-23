using CsvHelper.Configuration;
using MMIv8_Ktype.Models.Status;

namespace MMIv8_Ktype.CSV.Maps
{
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
