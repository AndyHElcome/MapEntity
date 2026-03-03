using MMIv8_Ktype.Models.DateIntersection;
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text;

namespace MMIv8_Ktype.Models.Indexes
{
    [Serializable]
    public class MongoSourceTecDocEngine(int kTypNr, IVersionProvider versionProvider) : SourceEntity(SourceIndex.TecDocEngine, kTypNr, versionProvider)
    {
        public int MotNr { get; set; }
        public string Make { get; set; }
        public string MCode { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonElement]
        public override string SourceEntityModelHash => GlobalHelpers.GenerateKey(new { this.Make, this.MCode });
        [BsonElement]
        public override string TextSort
        {
            get
            {
              
                return string.Empty;

            }
        }
        public int DFrom { get; set; }
        public int DTo { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonElement]
        public override DateOnlyRange DateRange => new(DFrom, DTo);

        [MongoDB.Bson.Serialization.Attributes.BsonRepresentation(BsonType.Decimal128)]
        public int Valves { get; set; }
        public int Cylinders { get; set; }
        public string Design { get; set; }
        public string FuelType { get; set; }
        public int NoOfCrankShaftBearings { get; set; }
        public decimal Bore { get; set; }
        public decimal Stroke { get; set; }
        public string Type { get; set; }
        public string CylinderDesign { get; set; }
        public string SalesDescription { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonElement]
        public override string EntityHash => GlobalHelpers.GenerateKey(new
        {
            this.MotNr,
            this.Make,
            this.MCode,
        });

    }
}
