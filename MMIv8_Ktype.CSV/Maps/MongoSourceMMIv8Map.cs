using System.Globalization;
using CsvHelper.Configuration;
using MMIv8_Ktype.CSV.Converters;
using MMIv8_Ktype.Models.Collections;

namespace MMIv8_Ktype.CSV.Maps
{
    public sealed class MongoSourceMMIv8Map : ClassMap<MongoSourceMMIv8>
    {
        public MongoSourceMMIv8Map()
        {
            AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.SourceEntityID).TypeConverter<ObjectIdConverter>();
            Map(m => m.MMI_V8_Key).Name("MMI V8 Key");
            Map(m => m.Mark_or_Series).Name("Mark or Series");
            Map(m => m.Engine_Size).TypeConverter<NAtoDecimalConverter>().Name("Engine Size");
            Map(m => m.Cylinders).TypeConverter<NAtoIntConverter>();
            Map(m => m.Cylinder_Layout).Name("Cylinder Layout");
            Map(m => m.Valve).TypeConverter<NAtoIntConverter>();
            Map(m => m.Start_Month).TypeConverter<NAtoIntConverter>().Name("Start Month");
            Map(m => m.Start_Year).TypeConverter<NAtoIntConverter>().Name("Start Year");
            Map(m => m.End_Month).TypeConverter<NAtoIntConverter>().Name("End Month");
            Map(m => m.End_Year).TypeConverter<NAtoIntConverter>().Name("End Year");
            Map(m => m.Doors).TypeConverter<NAtoIntConverter>();
            Map(m => m.Gears).TypeConverter<NAtoIntConverter>();
            Map(m => m.Exact_CC).TypeConverter<NAtoIntConverter>().Name("Exact CC");
            Map(m => m.BHP).TypeConverter<NAtoIntConverter>();
            Map(m => m.KW).TypeConverter<NAtoIntConverter>().Name("kW");
            Map(m => m.Doors).TypeConverter<NAtoIntConverter>();
            Map(m => m.Engine_Code).Name("Engine Code");

            Map(m => m.SourceEntityID).Ignore();
            Map(m => m.SourceIndex).Ignore();
            Map(m => m.Status).Ignore();
            Map(m => m.EntityHash).Ignore();
        }
    }
    
    public sealed class MMIv8AutoMap : ClassMap<MongoSourceMMIv8>
    {
        public MMIv8AutoMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.SourceEntityID).TypeConverter<ObjectIdConverter>();

            Map(m => m.DateRange.Start).Name("MMI_Start");
            Map(m => m.DateRange.End).Name("MMI_End");

            Map(m => m.SourceEntityID).Ignore();
            Map(m => m.SourceIndex).Ignore();
            Map(m => m.Status).Ignore();
            Map(m => m.EntityHash).Ignore();
            Map(m => m.SourceEntityModelHash).Ignore();

            Map(m => m.Start_Month).Ignore();
            Map(m => m.Start_Year).Ignore();
            Map(m => m.End_Month).Ignore();
            Map(m => m.End_Year).Ignore();
        }
    }
}
