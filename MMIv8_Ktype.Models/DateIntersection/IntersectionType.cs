namespace MMIv8_Ktype.Models.DateIntersection
{
    public enum IntersectionType
    {
        /// <summary>
        /// No Intersection
        /// </summary>
        None = -1,
        /// <summary>
        /// Given range ends inside the range
        /// </summary>
        EndsInRange,
        /// <summary>
        /// Given range starts inside the range
        /// </summary>
        StartsInRange,
        /// <summary>
        /// Both ranges are equaled
        /// </summary>
        RangesEqauled,
        /// <summary>
        /// Given range contained in the range
        /// </summary>
        ContainedInRange,
        /// <summary>
        /// Given range contains the range
        /// </summary>
        ContainsRange,
    }
}
