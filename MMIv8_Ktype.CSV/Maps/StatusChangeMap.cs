using CsvHelper.Configuration;
using MMIv8_Ktype.Models.Status;

namespace MMIv8_Ktype.CSV.Maps
{
    public sealed class StatusChangeMap : ClassMap<StatusChange>
    {
        public StatusChangeMap()
        {
            Map(m => m.Status);
            Map(m => m.DateOfChange);

            References<VersionMap>(m => m.Version);
        }
    }
}
