
namespace MMIv8_Ktype.Models.Outputs
{
    public class MatchResult
    {
        public int ComparisonCount { get; set; }
        public int MatchCount { get; set; }
        public bool PreviousMatch { get; set; } = false;
        public bool Failed { get; set; } = false;
        public string? FailDetail { get; set; }
        public int FailCount { get; set; }
        public int PerfectCount { get; set; }

        public bool IsPerfect => ComparisonCount == PerfectCount;

    }
}
