using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MMIv8_Ktype.Models.Outputs;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Models.Indexes;

namespace MMIv8_Ktype.Models.Collections
{
    public class MatchEntity : ICollectionEntity<ObjectId>, IStatusHistory
    {
        [BsonId]
        public ObjectId MatchEntityID { get; set; }
        public MongoSourceTecDocPC TecDocEntity { get; set; }
        public MongoSourceMMIv8 MMIv8Entity { get; set; }
        public ObjectId MatchMakeModelMatchID { get; set; }
        [BsonElement]
        public string RelationKey => $"{MMIv8Entity.MMI_V8_Key}-{TecDocEntity.KTypNr}";
        public StatusHistory Status { get; set; }

        [BsonElement]
        public DateIntersection.DateIntersection DateIntersection => new(TecDocEntity.DateRange, MMIv8Entity.DateRange);

        [BsonSerializer(typeof(EnumDictionarySerializer<MatchBaseType, Dictionary<MatchBaseType, MatchBase>>))]
        public Dictionary<MatchBaseType, MatchBase> EntityComparison { get; set; }

        public MatchResult MatchResult { get; set; } = new();
        public MatchRefine MatchRefine { get; set; } = new();
        public decimal? ScoreSum { get; set; }
        public decimal? ScoreAverage { get; set; }

        public bool IsBest => ScoreSum == MatchRefine.BestScore;
        public bool Matched { get; set; } = false;
        public string? MatchDetail { get; set; }

        [BsonIgnore]
        public ObjectId DocumentId => MatchEntityID;


        public MatchEntity(IVersionProvider versionProvider, MongoSourceTecDocPC tecdoc, MongoSourceMMIv8 mmiv8, ObjectId matchMakeModelMatchID)
        {
            Status = new(versionProvider);
            TecDocEntity = tecdoc;
            MMIv8Entity = mmiv8;
            MatchMakeModelMatchID = matchMakeModelMatchID;

            CalculateMatchBases(versionProvider);
        }

        public MatchEntity(IVersionProvider versionProvider, MongoSourceTecDocPC tecdoc, MongoSourceMMIv8 mmiv8, ObjectId matchMakeModelMatchID, bool previous = false)
        {
            Status = new(versionProvider);
            TecDocEntity = tecdoc;
            MMIv8Entity = mmiv8;
            MatchMakeModelMatchID = matchMakeModelMatchID;

            MatchResult.PreviousMatch = previous;

            CalculateMatchBases(versionProvider);
        }

        [Obsolete("Version Required")]
        public MatchEntity(MongoSourceTecDocPC tecdoc, MongoSourceMMIv8 mmiv8, ObjectId matchMakeModelMatchID)
        {
           throw new NotImplementedException("Version is now required");
        }

        public void CalculateMatchBases(IVersionProvider versionProvider)
        {
            EntityComparison = new List<(MatchBaseType, MatchBase)>()
            {
                {new MatchModel(versionProvider, this).MakePair() },
                {new MatchBody(versionProvider, this).MakePair() },
                {new MatchDrive(versionProvider, this).MakePair() },
                {new MatchFuel(versionProvider, this).MakePair() },
                {new MatchMark(versionProvider, this).MakePair() },
                {new MatchIdentifier(versionProvider, this).MakePair() },
                {new MatchCC(versionProvider, this).MakePair() },
                {new MatchValves(versionProvider, this).MakePair() },
                {new MatchCylinders(versionProvider, this).MakePair() },
                {new MatchBHP(versionProvider, this).MakePair() },
                {new MatchKW(versionProvider, this).MakePair() },
                {new MatchDate(versionProvider, this).MakePair() },
                //{new MatchDateRemainder(versionProvider, this).MakePair() },
                //{new MatchDateCoveredTD(versionProvider, this).MakePair() },
                //{new MatchDateCoveredMMI(versionProvider, this).MakePair() },
                {new MatchEngineCode(versionProvider, this).MakePair() },
                {new MatchRegion(versionProvider, this).MakePair() },
            }.ToDictionary();
        }

        [Obsolete("Don't use, instead use the MongoQueryVersion")]
        public void UpdateScore()
        {
            ScoreSum = EntityComparison.Sum(c => c.Value.Score);
            ScoreAverage = ScoreSum / EntityComparison.Count;
        }

    }
}
