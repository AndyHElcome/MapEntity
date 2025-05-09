using MMIv8_Ktype.Models.Collections;
using MongoDB.Bson;

namespace MMIv8_Ktype.Models.Outputs
{
    public class MatchRefine
    {
        public ObjectId[]? BestMatches { get; set; }
        public ObjectId[]? LastMatches { get; set; }
        public ObjectId[]? ChosenMatches { get; set; }
        public int PassCount { get; set; }
        public bool Difference { get; set; }
        public bool IsCheck { get; set; } = false;
        public decimal? BestScore { get; set; }

        public MatchRefine()
        { }

        public MatchRefine(MatchEntity[] matchEntities)
        {
            var passedMatches = matchEntities.Where(c => !c.MatchResult.Failed);

            BestScore = passedMatches.Max(c => c.ScoreSum);

            BestMatches = passedMatches.Where(c => c.ScoreSum == BestScore).Select(c => c.DocumentId).ToArray();
            IsCheck = passedMatches.Select(c => c.Status.Current.Status).Contains(Status.Status.Check);
            PassCount = passedMatches.Count();

            LastMatches = matchEntities.Where(c => c.MatchResult.PreviousMatch).Select(c => c.DocumentId).ToArray();
            ChosenMatches = matchEntities.Where(c => c.Matched).Select(c => c.DocumentId).ToArray();

            Difference = !BestMatches.SequenceEqual(LastMatches);
        }
    }
}
