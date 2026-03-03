using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MMIv8_Ktype.Models.Collections
{
    [BsonIgnoreExtraElements]
    public class MatchRefine : IEquatable<MatchRefine>
    {
        //[MongoDB.Bson.Serialization.Attributes.BsonElement("MMIv8EntityId")]
        //private ObjectId DocumentId { get; set; }
        public ObjectId[]? BestMatches { get; set; }
        public ObjectId[]? LastMatches { get; set; }
        public ObjectId[]? ChosenMatches { get; set; }
        public int PassCount { get; set; }
        public bool Difference { get; set; }
        public bool IsCheck { get; set; } = false;
        public decimal? BestScore { get; set; }

        public MatchRefine()
        { }

        public MatchRefine(List<MatchEntity> matchEntities)
        {
            var mmiv8EntityId = matchEntities.Select(c => c.MMIv8Entity.DocumentId).Distinct();

            //if (mmiv8EntityId.Count() > 1)
            //    DocumentId = ObjectId.Empty;
            //else
            //    DocumentId = mmiv8EntityId.FirstOrDefault();

            var passedMatches = matchEntities.Where(c => !c.MatchResult.Failed);

            BestScore = passedMatches.Max(c => c.ScoreSum);

            BestMatches = [ .. passedMatches.Where(c => c.ScoreSum == BestScore).Select(c => c.DocumentId) ];
            PassCount = passedMatches.Count();

            LastMatches = [ .. matchEntities.Where(c => c.MatchResult.PreviousMatch).Select(c => c.DocumentId) ];
            ChosenMatches = [ .. matchEntities.Where(c => c.Matched).Select(c => c.DocumentId) ];
            IsCheck = matchEntities.Select(c => c.Status.Current.Status).Contains(Status.Status.Check);

            Difference = !BestMatches.SequenceEqual(LastMatches);
        }

        public override bool Equals(object obj) => this.Equals(obj as MatchRefine);

        public bool Equals(MatchRefine other)
        {
            if (other is null)
                return false;

            if (Object.ReferenceEquals(this, other))
                return true;

            if (this.GetType() != other.GetType())
                return false;

            return (BestMatches == other.BestMatches) && (LastMatches == other.LastMatches) && (ChosenMatches == other.ChosenMatches)
                && (PassCount == other.PassCount) && (Difference == other.Difference) && (IsCheck == other.IsCheck) && (BestScore == other.BestScore);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(BestMatches, LastMatches, ChosenMatches, PassCount, Difference, IsCheck, BestScore);
        }

        public static bool operator ==(MatchRefine lhs, MatchRefine rhs)
        {
            if (lhs is null && rhs is null)
                return true;

            if (lhs is not null && rhs is not null)
                return lhs.Equals(rhs);

            return false;
        }

        public static bool operator !=(MatchRefine lhs, MatchRefine rhs) => !(lhs == rhs);
    }
}
