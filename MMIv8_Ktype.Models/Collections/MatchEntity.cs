using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MMIv8_Ktype.Models.Outputs;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Models.Indexes;
using System.Text.Json.Serialization;
using System.Dynamic;
using System.Xml;

namespace MMIv8_Ktype.Models.Collections
{
    [Serializable]
    public class MatchEntity : ICollectionEntity<ObjectId>, IStatusHistory
    {
        [BsonId]
        public ObjectId DocumentId { get; set; }
        public SourceTecDocPC TecDocEntity { get; set; }
        public SourceMMIv8 MMIv8Entity { get; set; }
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

        [BsonIgnore]
        public bool IsBest => MatchRefine.BestScore is not null && ScoreSum == MatchRefine.BestScore;
        public bool Matched { get; set; } = false;
        public string? MatchDetail { get; set; }

        public MatchEntity(IVersionProvider versionProvider, SourceTecDocPC tecdoc, SourceMMIv8 mmiv8, ObjectId matchMakeModelMatchID)
        {
            Status = new(versionProvider);
            Status.History.Push(versionProvider.NewStatus(Models.Status.Status.Check));

            TecDocEntity = tecdoc;
            MMIv8Entity = mmiv8;
            MatchMakeModelMatchID = matchMakeModelMatchID;

            CalculateMatchBases(versionProvider);
        }

        [Obsolete("Version Required")]
        public MatchEntity(SourceTecDocPC tecdoc, SourceMMIv8 mmiv8, ObjectId matchMakeModelMatchID)
        {
            throw new NotImplementedException("Version is now required");
        }

        [JsonConstructor]
        public MatchEntity()
        {
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
