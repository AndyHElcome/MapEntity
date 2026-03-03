using MMIv8_Ktype.Models.DateIntersection;
using MMIv8_Ktype.Models.Indexes;
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text;
using System.Text.Json.Serialization;

namespace MMIv8_Ktype.Models.Collections
{
    [Serializable]
    public class SourceMMIv8 : SourceEntity
    {
        public int MMI_V8_Key { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }

        [BsonElement]
        public override string SourceEntityModelHash => GlobalHelpers.GenerateKey(new { Manufacturer, Model });

        [BsonElement]
        public override string TextSort
        {
            get
            {
                var naturalSortParameters = new NaturalSortParameters
                {
                    StripDash = true,
                    StripSpace = true,
                    IgnoreDecimalPlaces = true
                };
                var stringBuilder = new StringBuilder()
                    .Append(NaturalSortGenerator.Generate(Manufacturer, naturalSortParameters)).Append('\t')
                    .Append(NaturalSortGenerator.Generate(Model, naturalSortParameters)).Append('\t')
                    .Append(NaturalSortGenerator.Generate(SubModel, naturalSortParameters)).Append('\t')
                    .Append(NaturalSortGenerator.Generate(Mark_or_Series, naturalSortParameters)).Append('\t')
                    .Append(NaturalSortGenerator.Generate(Token_Identifier, naturalSortParameters)).Append('\t')
                    .Append(NaturalSortGenerator.Generate(Body, naturalSortParameters)).Append('\t')
                    .Append(NaturalSortGenerator.Generate(Fuel, naturalSortParameters)).Append('\t')
                    .Append(NaturalSortGenerator.Generate(Engine_Size.ToString(), naturalSortParameters)).Append('\t')
                    .Append(NaturalSortGenerator.Generate(Engine_Code, naturalSortParameters));

                return stringBuilder.ToString();

            }
        }

        public string SubModel { get; set; }
        public string Mark_or_Series { get; set; }
        public string Identifier { get; set; }


        [BsonElement]
        public string Token_Identifier
        {
            get
            {
                string token_Identifier = GlobalHelpers.RemoveDuplicatedStrings(Identifier, [Model, SubModel, Engine_Size.ToString(),]);
                token_Identifier = GlobalHelpers.RemoveDuplicatedStringsTokenised(token_Identifier, [Mark_or_Series,]);
                return token_Identifier;
            }
        }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Engine_Size { get; set; }
        public int Cylinders { get; set; }
        public string Cylinder_Layout { get; set; }
        public string Cam { get; set; }
        public int Valve { get; set; }
        public int Start_Month { get; set; }
        public int Start_Year { get; set; }
        public int End_Month { get; set; }
        public int End_Year { get; set; }

        [BsonElement]
        public override DateOnlyRange DateRange => new(Start_Month, Start_Year, End_Month, End_Year);

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

        [BsonElement]
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

        public SourceMMIv8(int externalId, IVersionProvider versionProvider) : base(SourceIndex.MMIv8, externalId, versionProvider)
        {
        }

        [JsonConstructor]
        public SourceMMIv8() : base()
        {
        }
    }


}
