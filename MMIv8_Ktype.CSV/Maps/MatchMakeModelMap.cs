using CsvHelper.Configuration;
using MMIv8_Ktype.CSV.Converters;
using MMIv8_Ktype.Models.Collections;
using System.Globalization;

namespace MMIv8_Ktype.CSV.Maps
{
    public sealed class MatchMakeModelMap : ClassMap<MatchMakeModel>
    {
        public MatchMakeModelMap()
        {
            Map(m => m.MatchID).TypeConverter<ObjectIdConverter>().Name(nameof(MatchMakeModel.MatchID));
            References<MongoSourceEntityModelMap>(m => m.TecDocModel).Prefix("TD_");
            References<MongoSourceEntityModelMap>(m => m.MMIv8Model).Prefix("MMI_");

            References<StatusHistoryMap>(m => m.Status);
        }
    }
}
