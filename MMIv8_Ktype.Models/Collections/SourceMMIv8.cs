using MongoDB.Bson;
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
        public override string SourceEntityModelHash => GlobalHelpers.GenerateKey(new { Manufacturer, Model });
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
        public override DateTimeRange DateRange => new(Start_Month, Start_Year, End_Month, End_Year);

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
        public override int ExternalId => MMI_V8_Key;

        [MongoDB.Bson.Serialization.Attributes.BsonElement]
        public override string EntityHash => GlobalHelpers.GenerateKey(new
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
}
