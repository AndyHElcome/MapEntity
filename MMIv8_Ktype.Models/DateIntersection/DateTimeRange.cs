using System.Globalization;

namespace MMIv8_Ktype.Models.DateIntersection
{
    public struct DateTimeRange
    {

        #region Construction
        public DateTimeRange()
        {
            if (start > end)
            {
                throw new Exception("Invalid range edges.");
            }
        }
        public DateTimeRange(DateTime start, DateTime end) : this()
        {
            this.start = start;
            this.end = end;
        }

        /// <summary>
        /// Constructor for MMIv8 Date Format
        /// </summary>
        public DateTimeRange(int startMonth, int startYear, int endMonth, int endYear) : this()
        {
            DateTime start = DateTime.MinValue.Date;
            DateTime end = DateTime.MaxValue.Date;

            if (startMonth.ToString().Length <= 2 && startYear.ToString().Length == 4)
                start = new DateTime(startYear, startMonth, 01, 0, 0, 0, DateTimeKind.Utc).Date;


            if (endMonth.ToString().Length <= 2 && endYear.ToString().Length == 4)
                end = new DateTime(endYear, endMonth, 01, 0, 0, 0, DateTimeKind.Utc).Date;

            this.start = start;
            this.end = end;
        }

        /// <summary>
        /// Constructor for TecDoc Date Format
        /// </summary>
        public DateTimeRange(int dFrom, int dTo) : this()
        {
            DateTime start = DateTime.MinValue.Date;
            DateTime end = DateTime.MaxValue.Date;


            if (dFrom.ToString().Length == 6 && DateTime.TryParseExact(dFrom.ToString(), "yyyyMM", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime _start))
                start = _start.Date;

            if (dTo.ToString().Length == 6 && DateTime.TryParseExact(dTo.ToString(), "yyyyMM", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime _end))
                end = _end.Date;

            this.start = start;
            this.end = end;
        }
        #endregion

        #region Properties
        private DateTime start;

        [CsvHelper.Configuration.Attributes.Format("yyyy/MM/dd")]
        [MongoDB.Bson.Serialization.Attributes.BsonDateTimeOptions(DateOnly = true)]
        public DateTime Start
        {
            get { return start; }
            private set { start = value; }
        }
        private DateTime end;

        [CsvHelper.Configuration.Attributes.Format("yyyy/MM/dd")]
        [MongoDB.Bson.Serialization.Attributes.BsonDateTimeOptions(DateOnly = true)]
        public DateTime End
        {
            get { return end; }
            private set { end = value; }
        }
        #endregion

        #region Operators
        public static bool operator ==(DateTimeRange range1, DateTimeRange range2)
        {
            return range1.Equals(range2);
        }

        public static bool operator !=(DateTimeRange range1, DateTimeRange range2)
        {
            return !(range1 == range2);
        }
        public override bool Equals(object obj)
        {
            if (obj is DateTimeRange)
            {
                var range1 = this;
                var range2 = (DateTimeRange)obj;
                return range1.Start == range2.Start && range1.End == range2.End;
            }
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion

        #region Querying
        public bool Intersects(DateTimeRange range)
        {
            var type = GetIntersectionType(range);
            return type != IntersectionType.None;
        }

        public bool IsInRange(DateTime date)
        {
            return date >= Start && date <= End;
        }

        public IntersectionType GetIntersectionType(DateTimeRange range)
        {
            if (this == range)
            {
                return IntersectionType.RangesEqauled;
            }
            else if (IsInRange(range.Start) && IsInRange(range.End))
            {
                return IntersectionType.ContainedInRange;
            }
            else if (IsInRange(range.Start))
            {
                return IntersectionType.StartsInRange;
            }
            else if (IsInRange(range.End))
            {
                return IntersectionType.EndsInRange;
            }
            else if (range.IsInRange(Start) && range.IsInRange(End))
            {
                return IntersectionType.ContainsRange;
            }
            return IntersectionType.None;
        }

        public DateTimeRange GetIntersection(DateTimeRange range)
        {
            var type = GetIntersectionType(range);
            if (type == IntersectionType.RangesEqauled || type == IntersectionType.ContainedInRange)
            {
                return range;
            }
            else if (type == IntersectionType.StartsInRange)
            {
                return new DateTimeRange(range.Start, End);
            }
            else if (type == IntersectionType.EndsInRange)
            {
                return new DateTimeRange(Start, range.End);
            }
            else if (type == IntersectionType.ContainsRange)
            {
                return this;
            }
            else
            {
                return default;
            }
        }
        public int GetInverseIntersectionSpan(DateTimeRange range)
        {
            var currentStart = Math.Abs(Start.Year * 12 + Start.Month);
            var currentEnd = Math.Abs(End.Year * 12 + End.Month);

            var rangeStart = Math.Abs(range.Start.Year * 12 + range.Start.Month);
            var rangetEnd = Math.Abs(range.End.Year * 12 + range.End.Month);

            var startDifference = Math.Abs(currentStart - rangeStart);
            var endDifference = Math.Abs(currentEnd - rangetEnd);

            if (Start == DateTime.MinValue.Date || range.Start == DateTime.MinValue.Date)
            {
                startDifference = 0;
            }
            
            if (End == DateTime.MaxValue.Date || range.End == DateTime.MaxValue.Date)
            {
                endDifference = 0;
            }


            return startDifference + endDifference;
        }

        public int MonthDifference()
        {
            return (Start.Year - End.Year) * 12 + Start.Month - End.Month;
        }

        public int MonthDifferenceAbs()
        {
            return Math.Abs(MonthDifference());
        }

        public int GetIntersectionSpan(DateTimeRange range)
        {
            var intersection = GetIntersection(range);
            return intersection.MonthDifferenceAbs();
        }
        #endregion


        public override string ToString()
        {
            return Start.ToString() + " - " + End.ToString();
        }
    }
}
