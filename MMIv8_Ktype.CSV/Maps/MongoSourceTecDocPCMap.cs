using System.Globalization;
using CsvHelper.Configuration;
using MMIv8_Ktype.CSV.Converters;
using MMIv8_Ktype.Models.Collections;

namespace MMIv8_Ktype.CSV.Maps
{
    public sealed class MongoSourceTecDocPCMap : ClassMap<MongoSourceTecDocPC>
    {
        public MongoSourceTecDocPCMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.DocumentId).TypeConverter<ObjectIdConverter>();
            Map(m => m.DFrom).Name("dFrom").TypeConverter<NAtoIntConverter>();
            Map(m => m.DTo).Name("dTo").TypeConverter<NAtoIntConverter>();
            Map(m => m.KW).TypeConverter<NAtoIntConverter>();
            Map(m => m.PS).TypeConverter<NAtoIntConverter>();
            Map(m => m.Litre).TypeConverter<NAtoDecimalConverter>();
            Map(m => m.Cyl).TypeConverter<NAtoIntConverter>();
            Map(m => m.Drive).Name("4WD");
            Map(m => m.FuelType).Name("Fuel Type");
            Map(m => m.BodyType).Name("Body Type");
            Map(m => m.CCTech).Name("ccTech").TypeConverter<NAtoIntConverter>();
            Map(m => m.Valves).TypeConverter<NAtoIntConverter>();
            Map(m => m.Door).TypeConverter<NAtoIntConverter>();

            Map(m => m.DocumentId).Ignore();
            Map(m => m.SourceIndex).Ignore();
            Map(m => m.Status).Ignore();
            Map(m => m.EntityHash).Ignore();
        }
    }
    public sealed class TecDocAutoMap : ClassMap<MongoSourceTecDocPC>
    {
        public TecDocAutoMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.DocumentId).TypeConverter<ObjectIdConverter>();

            Map(m => m.DateRange.Start).Name("TD_Start");
            Map(m => m.DateRange.End).Name("TD_End");

            Map(m => m.DocumentId).Ignore();
            Map(m => m.SourceIndex).Ignore();
            Map(m => m.Status).Ignore();
            Map(m => m.EntityHash).Ignore();
            Map(m => m.SourceEntityModelHash).Ignore();

            Map(m => m.KModNr).Ignore();
            Map(m => m.Model).Ignore();
            Map(m => m.Type).Ignore();
            Map(m => m.DFrom).Ignore();
            Map(m => m.DTo).Ignore();
            Map(m => m.Exclude).Ignore();
        }
    }
}
