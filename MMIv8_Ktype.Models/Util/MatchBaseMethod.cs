namespace MMIv8_Ktype.Models.Util
{
    public enum MatchBaseMethod
    {
        /// <summary>
        /// The matches will never be stored / or written too.
        /// </summary>
        Automatic,
        /// <summary>
        /// The matches will not be stored but will be checked on MatchEntity creation, but can be stored upon request.
        /// </summary>
        Partial,
        /// <summary>
        /// The matches will always be stored and checked on MatchEntity creation.
        /// </summary>
        Manual
    }
}
