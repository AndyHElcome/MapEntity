using MMIv8_Ktype.Models.Attributes;
using MMIv8_Ktype.Models.DateIntersection;
using MMIv8_Ktype.Models.Indexes;
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Dynamic;
using System.Text.Json.Serialization;
using System.Xml;

namespace MMIv8_Ktype.Models.Collections
{
    [Serializable]
    public class SourceTecDocPC : SourceEntity
    {
        public int KTypNr { get; set; }
        public string Make { get; set; }
        public int KModNr { get; set; }
        public string Model { get; set; }

        [BsonElement]
        public string Token_Model
        {
            get
            {
                string token_Model = GlobalHelpers.RemoveDuplicatedStrings(Model, [SalesDesc, ModelGeneration, BodyType,]);
                return token_Model;
            }
        }

        public string Type { get; set; }

        [BsonElement]
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

        [BsonElement]
        public override string SourceEntityModelHash => GlobalHelpers.GenerateKey(new { Make, SalesDesc });
        public int DFrom { get; set; }
        public int DTo { get; set; }

        [BsonElement]
        public override DateTimeRange DateRange => new(DFrom, DTo);

        public int KW { get; set; }
        public int PS { get; set; }
        public int Calc_BHP => (int)(PS / 1.014 + .5);

        [BsonRepresentation(BsonType.Decimal128)]
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
        public string LinkedEngineCodes { get; set; }

        [BsonElement]
        [DoNotUpdateDifferences]
        public override string EntityHash => GlobalHelpers.GenerateKey(new
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

        public SourceTecDocPC(int externalId, IVersionProvider versionProvider) : base(SourceIndex.TecDocPC, externalId, versionProvider)
        {
        }

        [JsonConstructor]
        public SourceTecDocPC() : base()
        {
        }
    }
}
