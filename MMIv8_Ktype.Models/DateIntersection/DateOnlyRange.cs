namespace MMIv8_Ktype.Models.DateIntersection
{
    public struct DateOnlyRange
    {

        #region Construction
        public DateOnlyRange()
        {
            if (start > end)
            {
                throw new Exception("Invalid range edges.");
            }
        }
        public DateOnlyRange(DateOnly start, DateOnly end) : this()
        {
            this.start = start;
            this.end = end;
        }

        /// <summary>
        /// Constructor for MMIv8 Date Format
        /// </summary>
        public DateOnlyRange(int startMonth, int startYear, int endMonth, int endYear) : this()
        {
            DateOnly start = DateOnly.MinValue;
            DateOnly end = DateOnly.MaxValue;

            if (startMonth.ToString().Length <= 2 && startYear.ToString().Length == 4)
                start = new DateOnly(startYear, startMonth, 01);


            if (endMonth.ToString().Length <= 2 && endYear.ToString().Length == 4)
                end = new DateOnly(endYear, endMonth, 01);

            this.start = start;
            this.end = end;
        }

        /// <summary>
        /// Constructor for TecDoc Date Format
        /// </summary>
        public DateOnlyRange(int dFrom, int dTo) : this()
        {
            DateOnly start = DateOnly.MinValue;
            DateOnly end = DateOnly.MaxValue;


            if (dFrom.ToString().Length == 6 && DateOnly.TryParseExact(dFrom.ToString(), "yyyyMM", out DateOnly _start))
                start = _start;

            if (dTo.ToString().Length == 6 && DateOnly.TryParseExact(dTo.ToString(), "yyyyMM", out DateOnly _end))
                end = _end;

            this.start = start;
            this.end = end;
        }
        #endregion

        #region Properties
        private DateOnly start;

        public DateOnly Start
        {
            get { return start; }
            private set { start = value; }
        }
        private DateOnly end;

        public DateOnly End
        {
            get { return end; }
            private set { end = value; }
        }
        #endregion

        #region Operators
        public static bool operator ==(DateOnlyRange range1, DateOnlyRange range2)
        {
            return range1.Equals(range2);
        }

        public static bool operator !=(DateOnlyRange range1, DateOnlyRange range2)
        {
            return !(range1 == range2);
        }
        public override bool Equals(object obj)
        {
            if (obj is DateOnlyRange)
            {
                var range1 = this;
                var range2 = (DateOnlyRange)obj;
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
        public bool Intersects(DateOnlyRange range)
        {
            var type = GetIntersectionType(range);
            return type != IntersectionType.None;
        }

        public bool IsInRange(DateOnly date)
        {
            return date >= Start && date <= End;
        }

        public IntersectionType GetIntersectionType(DateOnlyRange range)
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

        public DateOnlyRange GetIntersection(DateOnlyRange range)
        {
            var type = GetIntersectionType(range);
            if (type == IntersectionType.RangesEqauled || type == IntersectionType.ContainedInRange)
            {
                return range;
            }
            else if (type == IntersectionType.StartsInRange)
            {
                return new DateOnlyRange(range.Start, End);
            }
            else if (type == IntersectionType.EndsInRange)
            {
                return new DateOnlyRange(Start, range.End);
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
        public int GetInverseIntersectionSpan(DateOnlyRange range)
        {
            int startDifference;
            if (Start == DateOnly.MinValue || range.Start == DateOnly.MinValue)
            {
                startDifference = 0;
            }
            else
            {
                var currentStart = Math.Abs(Start.Year * 12 + Start.Month);
                var rangeStart = Math.Abs(range.Start.Year * 12 + range.Start.Month);
                startDifference = Math.Abs(currentStart - rangeStart);
            }

            int endDifference;
            if (End == DateOnly.MaxValue || range.End == DateOnly.MaxValue)
            {
                endDifference = 0;
            }
            else
            {
                var currentEnd = Math.Abs(End.Year * 12 + End.Month);
                var rangetEnd = Math.Abs(range.End.Year * 12 + range.End.Month);
                endDifference = Math.Abs(currentEnd - rangetEnd);
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

        public int GetIntersectionSpan(DateOnlyRange range)
        {
            var intersection = GetIntersection(range);
            return intersection.MonthDifferenceAbs();
        }
        #endregion


        public override string ToString()
        {
            return Start.ToString("MM/yyyy") + " - " + End.ToString("MM/yyyy");
        }
    }
}
