using CsvHelper.Configuration;
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using System.Globalization;
using MMIv8_Ktype.Models.Outputs;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Models.DateIntersection;

namespace MMIv8_Ktype.Models.Collections
{
    public class MatchEntity : IStatusHistory
    {
        [BsonId]
        [CsvHelper.Configuration.Attributes.Ignore]
        public ObjectId MatchEntityID { get; set; }

        public MongoSourceTecDocPC TecDocEntity { get; set; }
        public MongoSourceMMIv8 MMIv8Entity { get; set; }

        [CsvHelper.Configuration.Attributes.Ignore]
        public ObjectId MatchMakeModelMatchID { get; set; }

        public StatusHistory Status { get; set; }

        [BsonElement]
        public MMIv8_Ktype.Models.DateIntersection.DateIntersection DateIntersection => new(TecDocEntity.DateRange, MMIv8Entity.DateRange);

        [BsonSerializer(typeof(EnumDictionarySerializer<MatchBaseType, Dictionary<MatchBaseType, MatchBase>>))]
        public Dictionary<MatchBaseType, MatchBase> EntityComparison { get; set; }

        public MatchResult MatchResult { get; set; } = new();
        public MatchRefine MatchRefine { get; set; } = new();
        public decimal? ScoreSum { get; set; }
        public decimal? ScoreAverage { get; set; }

        public bool IsBest => ScoreSum == MatchRefine.BestScore;
        public bool Matched { get; set; } = false;
        public string? MatchDetail { get; set; }


        public MatchEntity(IVersionProvider versionProvider, MongoSourceTecDocPC tecdoc, MongoSourceMMIv8 mmiv8, ObjectId matchMakeModelMatchID)
        {
            Status = new(versionProvider);
            TecDocEntity = tecdoc;
            MMIv8Entity = mmiv8;
            MatchMakeModelMatchID = matchMakeModelMatchID;

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

    public sealed class TecDocAutoMap : ClassMap<MongoSourceTecDocPC>
    {
        public TecDocAutoMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.SourceEntityID).TypeConverter<ObjectIdConverter>();

            Map(m => m.DateRange.Start).Name("TD_Start");
            Map(m => m.DateRange.End).Name("TD_End");

            Map(m => m.KModNr).Ignore();
            Map(m => m.Model).Ignore();
            Map(m => m.Type).Ignore();
            Map(m => m.DFrom).Ignore();
            Map(m => m.DTo).Ignore();
            Map(m => m.Exclude).Ignore();
            Map(m => m.SourceEntityModelHash).Ignore();
            Map(m => m.EntityHash).Ignore();
        }
    }

    public sealed class MMIv8AutoMap : ClassMap<MongoSourceMMIv8>
    {
        public MMIv8AutoMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.SourceEntityID).TypeConverter<ObjectIdConverter>();

            Map(m => m.DateRange.Start).Name("MMI_Start");
            Map(m => m.DateRange.End).Name("MMI_End");


            Map(m => m.Start_Month).Ignore();
            Map(m => m.Start_Year).Ignore();
            Map(m => m.End_Month).Ignore();
            Map(m => m.End_Year).Ignore();
            Map(m => m.SourceEntityModelHash).Ignore();
            Map(m => m.EntityHash).Ignore();
        }
    }

    public sealed class MatchEntityMap : ClassMap<MatchEntity>
    {
        public MatchEntityMap()
        {
            //AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.MatchEntityID).TypeConverter<ObjectIdConverter>();
            Map(m => m.MatchMakeModelMatchID).TypeConverter<ObjectIdConverter>();

            References<TecDocAutoMap>(m => m.TecDocEntity).Prefix("TD_");
            References<MMIv8AutoMap>(m => m.MMIv8Entity).Prefix("MMI_");

            foreach (var key in (MatchBaseType[])Enum.GetValues(typeof(MatchBaseType)))
            {
                Map(m => m.EntityComparison, false).Name($"{key.ToString()}_MatchHash").Convert(args =>
                {
                    var dict = args.Value.EntityComparison;
                    return dict != null && dict.ContainsKey(key) ? dict[key].MatchHash : string.Empty;
                });

                Map(m => m.EntityComparison, false).Name($"{key.ToString()}_Score").Convert(args =>
                {
                    var dict = args.Value.EntityComparison;
                    return dict != null && dict.ContainsKey(key) ? dict[key].Score.ToString() : string.Empty;
                });
            }

            References<DateIntersectionMap>(m => m.DateIntersection);
            References<StatusHistoryMap>(m => m.Status);
            References<MatchResultMap>(m => m.MatchResult);
            References<MatchRefineMap>(m => m.MatchRefine);
            Map(m => m.IsBest);

            Map(m => m.ScoreSum);
            Map(m => m.ScoreAverage);
            Map(m => m.Matched);
            Map(m => m.MatchDetail);
        }
    }
}
