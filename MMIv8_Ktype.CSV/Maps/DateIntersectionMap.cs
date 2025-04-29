using System.Globalization;
using CsvHelper.Configuration;
using MMIv8_Ktype.Models.DateIntersection;

namespace MMIv8_Ktype.CSV.Maps
{
    public sealed class DateIntersectionMap : ClassMap<DateIntersection>
    {
        public DateIntersectionMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
        }
    }
    public sealed class DateTimeRangeMap : ClassMap<DateTimeRange>
    {
        public DateTimeRangeMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.Start).TypeConverterOption.Format("yyyy/MM/dd");
            Map(m => m.End).TypeConverterOption.Format("yyyy/MM/dd");
        }
    }
}
