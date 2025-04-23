using System.Globalization;
using CsvHelper.Configuration;
using MMIv8_Ktype.Models.Collections;

namespace MMIv8_Ktype.CSV.Maps
{
    public sealed class MongoSourceEntityModelMap : ClassMap<MongoSourceEntityModel>
    {
        public MongoSourceEntityModelMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
        }
    }
}
