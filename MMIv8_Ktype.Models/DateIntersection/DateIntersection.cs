using CsvHelper.Configuration;
using System.Globalization;

namespace MMIv8_Ktype.Models.DateIntersection
{
    public class DateIntersection
    {
        public DateIntersection(DateTimeRange TD_Date, DateTimeRange MMI_Date)
        {
            td_Date = TD_Date;
            mmi_Date = MMI_Date;
        }

        private DateTimeRange td_Date { get; set; }

        private DateTimeRange mmi_Date { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonElement]
        public int date_Intersection => mmi_Date.GetIntersectionSpan(td_Date);

        [MongoDB.Bson.Serialization.Attributes.BsonElement]
        public int date_InverseIntersection => mmi_Date.GetInverseIntersectionSpan(td_Date);

        [MongoDB.Bson.Serialization.Attributes.BsonElement]
        public IntersectionType date_TD_IntersectionType => td_Date.GetIntersectionType(mmi_Date);

        [MongoDB.Bson.Serialization.Attributes.BsonElement]
        private int date_TD_Span => td_Date.MonthDifferenceAbs();

        [MongoDB.Bson.Serialization.Attributes.BsonElement]
        public double date_TD_Coverage => Math.Round(date_Intersection * 100.0 / date_TD_Span, 2);

        [MongoDB.Bson.Serialization.Attributes.BsonElement]
        public IntersectionType date_MMI_IntersectionType => mmi_Date.GetIntersectionType(td_Date);

        [MongoDB.Bson.Serialization.Attributes.BsonElement]
        private int date_MMI_Span => mmi_Date.MonthDifferenceAbs();

        [MongoDB.Bson.Serialization.Attributes.BsonElement]
        public double date_MMI_Coverage => Math.Round(date_Intersection * 100.0 / date_MMI_Span, 2);
    }

    public sealed class DateIntersectionMap : ClassMap<DateIntersection>
    {
        public DateIntersectionMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
        }
    }
}
