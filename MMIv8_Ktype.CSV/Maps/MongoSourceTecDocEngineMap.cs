using System.Globalization;
using CsvHelper.Configuration;
using MMIv8_Ktype.CSV.Converters;
using MMIv8_Ktype.Models.Indexes;

namespace MMIv8_Ktype.CSV.Maps
{
    public sealed class MongoSourceTecDocEngineMap : ClassMap<MongoSourceTecDocEngine>
    {
        public MongoSourceTecDocEngineMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.SourceEntityID).TypeConverter<ObjectIdConverter>();
            Map(m => m.DFrom).Name("dFrom").TypeConverter<NAtoIntConverter>();
            Map(m => m.DTo).Name("dTo").TypeConverter<NAtoIntConverter>();
            Map(m => m.Valves).TypeConverter<NAtoIntConverter>();
            Map(m => m.Cylinders).TypeConverter<NAtoIntConverter>();
            Map(m => m.NoOfCrankShaftBearings).TypeConverter<NAtoIntConverter>();
            Map(m => m.Bore).TypeConverter<NAtoDecimalConverter>();
            Map(m => m.Stroke).TypeConverter<NAtoDecimalConverter>();

            Map(m => m.SourceEntityID).Ignore();
            Map(m => m.SourceIndex).Ignore();
            Map(m => m.Status).Ignore();
            Map(m => m.EntityHash).Ignore();
        }
    }
}
