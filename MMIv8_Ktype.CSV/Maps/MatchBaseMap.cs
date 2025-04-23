using CsvHelper.Configuration;
using MMIv8_Ktype.Models.Collections;
using System.Globalization;

namespace MMIv8_Ktype.CSV.Maps
{
    public sealed class MatchBaseMap : ClassMap<MatchBase>
    {
        public MatchBaseMap()
        {
            AutoMap(CultureInfo.InvariantCulture);

            References<StatusHistoryMap>(m => m.Status);
            Map(m => m.MatchContexts).Ignore();
        }
    }
    
    public sealed class MatchBaseScoreMap : ClassMap<MatchBase>
    {
        public MatchBaseScoreMap()
        {
            Map(m => m.Score);
        }
    }
}
