using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MongoDB.Bson;
using System.Globalization;
using MMIv8_Ktype.Models.Indexes;
using MMIv8_Ktype.Models.DateIntersection;

namespace MMIv8_Ktype.Models.Collections
{
    [Serializable]
    public class MongoSourceMMIv8 : SourceEntity
    {
        public MongoSourceMMIv8(IVersionProvider versionProvider) : base(versionProvider)
        {
            SourceEntityID = ObjectId.GenerateNewId();
            SourceIndex = SourceIndex.MMIv8;
        }

        public int MMI_V8_Key { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonElement]
        public string SourceEntityModelHash => GlobalHelpers.GenerateKey(new { Manufacturer, Model });
        public string SubModel { get; set; }
        public string Mark_or_Series { get; set; }
        public string Identifier { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonElement]
        public string Token_Identifier
        {
            get
            {
                string token_Identifier = GlobalHelpers.RemoveDuplicatedStrings(Identifier, [Model, SubModel, Engine_Size.ToString(),]);
                token_Identifier = GlobalHelpers.RemoveDuplicatedStringsTokenised(token_Identifier, [Mark_or_Series,]);
                return token_Identifier;
            }
        }

        [MongoDB.Bson.Serialization.Attributes.BsonRepresentation(BsonType.Decimal128)]
        public decimal Engine_Size { get; set; }
        public int Cylinders { get; set; }
        public string Cylinder_Layout { get; set; }
        public string Cam { get; set; }
        public int Valve { get; set; }
        public int Start_Month { get; set; }
        public int Start_Year { get; set; }
        public int End_Month { get; set; }
        public int End_Year { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonElement]
        public DateTimeRange DateRange => new(Start_Month, Start_Year, End_Month, End_Year);

        public string Body { get; set; }
        public int Doors { get; set; }
        public string Transmission { get; set; }
        public int Gears { get; set; }
        public int Exact_CC { get; set; }
        public string Drive { get; set; }
        public string Fuel { get; set; }
        public int BHP { get; set; }
        public int KW { get; set; }
        public string Engine_Code { get; set; }
        public string EntityHash => GlobalHelpers.GenerateKey(new
        {
            MMI_V8_Key,
            Manufacturer,
            Model,
            SubModel,
            Mark_or_Series,
            Identifier,
            Engine_Size,
            Cylinders,
            Cylinder_Layout,
            Cam,
            Valve,
            Start_Month,
            Start_Year,
            End_Month,
            End_Year,
            Body,
            Doors,
            Transmission,
            Gears,
            Exact_CC,
            Drive,
            Fuel,
            BHP,
            KW,
            Engine_Code,
        });
    }

    public sealed class MongoSourceMMIv8Map : ClassMap<MongoSourceMMIv8>
    {
        public MongoSourceMMIv8Map()
        {
            AutoMap(CultureInfo.InvariantCulture);
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

            Map(m => m.EntityHash).Ignore();
        }
    }



    public class NAtoIntConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (int.TryParse(text, out int convertedText))
            {
                return convertedText;
            }
            else
            {
                return 0;
            }
        }
    }

    public class NAtoDecimalConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (decimal.TryParse(text, out decimal convertedText))
            {
                return convertedText;
            }
            else
            {
                return (decimal)0;
            }
        }
    }
}
