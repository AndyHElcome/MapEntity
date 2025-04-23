using CsvHelper.Configuration;
using Version = MMIv8_Ktype.Models.Collections.Version;

namespace MMIv8_Ktype.CSV.Maps
{
    public sealed class VersionMap : ClassMap<Version>
    {
        public VersionMap()
        {
            Map(m => m.VersionID).Ignore();
            Map(m => m.VersionNumber);
            Map(m => m.TecDocEntityVersion);
            Map(m => m.MMIv8EntityVersion);
            References<UserMap>(m => m.User);
        }
    }
}
