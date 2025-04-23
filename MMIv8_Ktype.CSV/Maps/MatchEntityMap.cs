using MMIv8_Ktype.Models.Util;
using MMIv8_Ktype.Models.Outputs;
using MMIv8_Ktype.Models.Collections;
using CsvHelper.Configuration;
using System.Globalization;
using MMIv8_Ktype.CSV.Converters;

namespace MMIv8_Ktype.CSV.Maps
{
    public sealed class MatchEntityMap : ClassMap<MatchEntity>
    {
        public MatchEntityMap()
        {
            //AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.MatchEntityID).Ignore();
            Map(m => m.MatchEntityID).TypeConverter<ObjectIdConverter>();
            Map(m => m.MatchMakeModelMatchID).TypeConverter<ObjectIdConverter>();

            References<TecDocAutoMap>(m => m.TecDocEntity).Prefix("TD_");
            References<MMIv8AutoMap>(m => m.MMIv8Entity).Prefix("MMI_");

            foreach (var key in (MatchBaseType[])Enum.GetValues(typeof(MatchBaseType)))
            {
                Map(m => m.EntityComparison, false).Name($"{key.ToString()}_MatchHash").Convert(args =>
                {
                    var dict = args.Value.EntityComparison;
                    return dict != null && dict.ContainsKey(key) ? dict[key].MatchHash : string.Empty;
                });

                Map(m => m.EntityComparison, false).Name($"{key.ToString()}_Score").Convert(args =>
                {
                    var dict = args.Value.EntityComparison;
                    return dict != null && dict.ContainsKey(key) ? dict[key].Score.ToString() : string.Empty;
                });
            }

            References<DateIntersectionMap>(m => m.DateIntersection);
            References<StatusHistoryMap>(m => m.Status);
            References<MatchResultMap>(m => m.MatchResult);
            References<MatchRefineMap>(m => m.MatchRefine);
            Map(m => m.IsBest);

            Map(m => m.ScoreSum);
            Map(m => m.ScoreAverage);
            Map(m => m.Matched);
            Map(m => m.MatchDetail);
        }
    }

    public sealed class MatchRefineMap : ClassMap<MatchRefine>
    {
        public MatchRefineMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
        }
    }

    public sealed class MatchResultMap : ClassMap<MatchResult>
    {
        public MatchResultMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
        }
    }
}
