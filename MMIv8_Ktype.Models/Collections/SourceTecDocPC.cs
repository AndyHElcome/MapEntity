using CsvHelper.Configuration;
using MMIv8_Ktype.Models.DateIntersection;
using MMIv8_Ktype.Models.Indexes;
using MongoDB.Bson;
using System.Globalization;

namespace MMIv8_Ktype.Models.Collections
{
    [Serializable]
    public class MongoSourceTecDocPC : SourceEntity
    {
        public MongoSourceTecDocPC(IVersionProvider versionProvider) : base(versionProvider)
        {
            SourceEntityID = ObjectId.GenerateNewId();
            SourceIndex = SourceIndex.TecDocPC;
        }

        public int KTypNr { get; set; }
        public string Make { get; set; }
        public int KModNr { get; set; }
        public string Model { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonElement]
        public string Token_Model
        {
            get
            {
                string token_Model = GlobalHelpers.RemoveDuplicatedStrings(Model, [SalesDesc, ModelGeneration, BodyType,]);
                return token_Model;
            }
        }

        public string Type { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonElement]
        public string Token_Type
        {
            get
            {
                string[] stringsToRemove = [Litre.ToString(), TypeDesc, Drive,];
                stringsToRemove = Drive switch
                {
                    "All-wheel Drive" => [.. stringsToRemove, "AWD", "4WD", "4x4"],
                    "Rear-Wheel Drive" => [.. stringsToRemove, "RWD"],
                    "Front-Wheel Drive" => [.. stringsToRemove, "FWD"],
                    _ => stringsToRemove,
                };

                string token_Type = GlobalHelpers.RemoveDuplicatedStrings(Type, stringsToRemove);
                return token_Type;
            }
        }

        [MongoDB.Bson.Serialization.Attributes.BsonElement]
        public string SourceEntityModelHash => GlobalHelpers.GenerateKey(new { Make, SalesDesc });
        public int DFrom { get; set; }
        public int DTo { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonElement]
        public DateTimeRange DateRange => new(DFrom, DTo);

        public int KW { get; set; }
        public int PS { get; set; }
        public int Calc_BHP => (int)(PS / 1.014 + .5);

        [MongoDB.Bson.Serialization.Attributes.BsonRepresentation(BsonType.Decimal128)]
        public decimal Litre { get; set; }
        public int Valves { get; set; }
        public int Cyl { get; set; }
        public int Calc_Valve => Cyl * Valves;
        public string Drive { get; set; }
        public string FuelType { get; set; }
        public string BodyType { get; set; }
        public int CCTech { get; set; }
        public string SalesDesc { get; set; }
        public string ModelGeneration { get; set; }
        public string TypeDesc { get; set; }
        public bool Exclude { get; set; }
        public int Door { get; set; }
        public string Region { get; set; }

        //[MongoDB.Bson.Serialization.Attributes.BsonElement]
        //public List<MongoSourceTecDocEngine> LinkedEngines { get; set; }

        //[MongoDB.Bson.Serialization.Attributes.BsonElement]
        //public string LinkedEngineCodes => string.Join('|', LinkedEngines.Select(c => c.MCode).Distinct().Order());
        public string LinkedEngineCodes { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonElement]
        public string EntityHash => GlobalHelpers.GenerateKey(new
        {
            KTypNr,
            Make,
            KModNr,
            Model,
            Type,
            DFrom,
            DTo,
            KW,
            PS,
            Litre,
            Valves,
            Cyl,
            Drive,
            FuelType,
            BodyType,
            CCTech,
            SalesDesc,
            ModelGeneration,
            TypeDesc,
            Exclude,
            Door,
            Region,
            LinkedEngineCodes,
        });

    }




    public sealed class MongoSourceTecDocPCMap : ClassMap<MongoSourceTecDocPC>
    {
        public MongoSourceTecDocPCMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
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

            Map(m => m.EntityHash).Ignore();
            //Map(m => m.LinkedEngines).Ignore();
        }
    }
}
