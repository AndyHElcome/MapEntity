using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Dynamic;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MMIv8_Ktype.Models.Collections
{
    //TODO Implement this: https://youtu.be/v6cYTcEfZ8A?si=kwpt3lbMri5HZ89E

    [BsonDiscriminator(Required = true)]
    [BsonKnownTypes(typeof(MatchModel), typeof(MatchMark), typeof(MatchIdentifier), typeof(MatchEngineCode),
                    typeof(MatchBody), typeof(MatchDrive), typeof(MatchFuel),
                    typeof(MatchCC), typeof(MatchCylinders), typeof(MatchValves), typeof(MatchBHP), typeof(MatchKW), typeof(MatchRegion), typeof(MatchDate))]
    public class MatchBase : ICollectionEntity<string>, IStatusHistory
    {
        [BsonId]
        public string DocumentId { get; set; }
        public MatchBaseType MatchBaseType { get; set; }
        public MatchBaseMethod MatchBaseMethod { get; set; }
        public double DefaultScore { get; set; }
        public Dictionary<string, dynamic> TecDocEntity { get; set; } = new();
        public Dictionary<string, dynamic> MMIEntity { get; set; } = new();

        [BsonDefaultValue(null)]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Score { get; set; }

        [BsonDefaultValue(null)]
        public bool? Overridden { get; set; }

        [BsonDefaultValue(null)]
        public string? OverriddenBy { get; set; }

        [BsonIgnoreIfDefault]
        public List<MatchContext>? MatchContexts { get; set; }

        public StatusHistory Status { get; set; }

        public MatchBase(IVersionProvider versionProvider, MatchEntity matchEntity, MatchBaseType matchBaseType, MatchBaseMethod matchBaseMethod, double defaultScore = 1.1)
        {
            Status = new(versionProvider);
            MatchBaseType = matchBaseType;
            MatchBaseMethod = matchBaseMethod;
            DefaultScore = defaultScore;

            TecDocEntity = CreateTecDocEntity(matchEntity);
            MMIEntity = CreateMMIEntity(matchEntity);
            DocumentId = GenerateMatchKey();
            Score = CalculateScore();
        }

        [Obsolete()]
        public MatchBase()
        {
        }

        public virtual decimal CalculateScore() => throw new NotImplementedException();
        public virtual Dictionary<string, dynamic> CreateTecDocEntity(MatchEntity matchEntity) => throw new NotImplementedException();
        public virtual Dictionary<string, dynamic> CreateMMIEntity(MatchEntity matchEntity) => throw new NotImplementedException();

        public string GenerateMatchKey() => GlobalHelpers.GenerateKey(new { MatchBaseType, TecDocEntity, MMIEntity });

        public (MatchBaseType, MatchBase) MakePair() => new(MatchBaseType, this);

        public MatchBase Reset(IVersionProvider versionProvider)
        {
            MatchContexts = null;
            Status = new(versionProvider);
            Score = CalculateScore();
            return this;
        }

        public override string ToString()
        {
            string tecdocEntity = TecDocEntity.DictToString();
            string mmiEntity = MMIEntity.DictToString();
            return $"TD: [{tecdocEntity}] MMI: [{mmiEntity}] Score: {Score} [{MatchBaseType}-{DocumentId}]";
        }

        public dynamic BuildCsvObject()
        {
            dynamic csvObj = new ExpandoObject();

            csvObj.MatchHash = DocumentId;
            csvObj.MatchBaseType = MatchBaseType;


            foreach (var p in TecDocEntity)
            {
                GlobalHelpers.AddProperty(csvObj, $"TecDocEntity.{p.Key}", p.Value);
            }
            foreach (var p in MMIEntity)
            {
                GlobalHelpers.AddProperty(csvObj, $"MMIEntity.{p.Key}", p.Value);
            }

            csvObj.Score = Score;
            csvObj.Status = Status.Current?.Status;

            csvObj.Contexts = MatchContexts is null || MatchContexts.Count == 0 ? "" : JsonSerializer.Serialize(MatchContexts);

            return csvObj;
        }
    }

    public static class MatchBaseExtentions
    {
        public static string DictToString(this Dictionary<string, dynamic> dict, string separator = " ")
        {
            return string.Join(separator, dict.Select(c => c.Value.ToString()).Where(c => !string.IsNullOrWhiteSpace(c)));
        }
        public static string DictToStringWithKey(this Dictionary<string, dynamic> dict, string separator = " ")
        {
            return string.Join(separator, dict.Select(c => $"{c.Key.ToString()}: {c.Value.ToString()}").Where(c => !string.IsNullOrWhiteSpace(c)));
        }
    }

    #region Manual Matches
    public class MatchBody(IVersionProvider versionProvider, MatchEntity matchEntity) : MatchBase(versionProvider, matchEntity, MatchBaseType.Body, MatchBaseMethod.Manual)
    {
        public override Dictionary<string, dynamic> CreateTecDocEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.TecDocEntity.BodyType,
            });

        public override Dictionary<string, dynamic> CreateMMIEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.MMIv8Entity.Body,
            });

        public override decimal CalculateScore() => MatchBaseScoreExtensions.CalculateScoreText([TecDocEntity[nameof(MatchEntity.TecDocEntity.BodyType)]], [MMIEntity[nameof(MatchEntity.MMIv8Entity.Body)]]);
    }

    public class MatchDrive(IVersionProvider versionProvider, MatchEntity matchEntity) : MatchBase(versionProvider, matchEntity, MatchBaseType.Drive, MatchBaseMethod.Manual)
    {
        public override Dictionary<string, dynamic> CreateTecDocEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.TecDocEntity.Drive,
            });

        public override Dictionary<string, dynamic> CreateMMIEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.MMIv8Entity.Drive,
            });

        public override decimal CalculateScore() => MatchBaseScoreExtensions.CalculateScoreText([TecDocEntity[nameof(MatchEntity.TecDocEntity.Drive)]], [MMIEntity[nameof(MatchEntity.MMIv8Entity.Drive)]]);
    }

    public class MatchFuel(IVersionProvider versionProvider, MatchEntity matchEntity) : MatchBase(versionProvider, matchEntity, MatchBaseType.Fuel, MatchBaseMethod.Manual)
    {
        public override Dictionary<string, dynamic> CreateTecDocEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.TecDocEntity.FuelType,
            });

        public override Dictionary<string, dynamic> CreateMMIEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.MMIv8Entity.Fuel,
            });

        public override decimal CalculateScore() => MatchBaseScoreExtensions.CalculateScoreText([TecDocEntity[nameof(MatchEntity.TecDocEntity.FuelType)]], [MMIEntity[nameof(MatchEntity.MMIv8Entity.Fuel)]]);
    }
    #endregion

    #region Partial Matches
    public class MatchModel(IVersionProvider versionProvider, MatchEntity matchEntity) : MatchBase(versionProvider, matchEntity, MatchBaseType.Model, MatchBaseMethod.Partial)
    {
        public override Dictionary<string, dynamic> CreateTecDocEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.TecDocEntity.Make,
                matchEntity.TecDocEntity.SalesDesc
            });

        public override Dictionary<string, dynamic> CreateMMIEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.MMIv8Entity.Manufacturer,
                matchEntity.MMIv8Entity.Model
            });

        public override decimal CalculateScore() => Convert.ToDecimal(DefaultScore);
    }

    public class MatchMark(IVersionProvider versionProvider, MatchEntity matchEntity) : MatchBase(versionProvider, matchEntity, MatchBaseType.Mark, MatchBaseMethod.Partial, 0.8)
    {
        public override Dictionary<string, dynamic> CreateTecDocEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.TecDocEntity.Make,
                matchEntity.TecDocEntity.SalesDesc,
                matchEntity.TecDocEntity.Token_Model,
                matchEntity.TecDocEntity.ModelGeneration
            });

        public override Dictionary<string, dynamic> CreateMMIEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.MMIv8Entity.Manufacturer,
                matchEntity.MMIv8Entity.Model,
                matchEntity.MMIv8Entity.Mark_or_Series
            });

        public override decimal CalculateScore()
        {
            return MatchBaseScoreExtensions.CalculateScoreText(
                [TecDocEntity[nameof(MatchEntity.TecDocEntity.Token_Model)], TecDocEntity[nameof(MatchEntity.TecDocEntity.ModelGeneration)]],
                [MMIEntity[nameof(MatchEntity.MMIv8Entity.Mark_or_Series)]],
                DefaultScore);
        }
    }

    public class MatchIdentifier(IVersionProvider versionProvider, MatchEntity matchEntity) : MatchBase(versionProvider, matchEntity, MatchBaseType.Identifier, MatchBaseMethod.Partial, 0.8)
    {
        public override Dictionary<string, dynamic> CreateTecDocEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.TecDocEntity.Make,
                matchEntity.TecDocEntity.SalesDesc,
                matchEntity.TecDocEntity.Token_Model,
                matchEntity.TecDocEntity.ModelGeneration,
                matchEntity.TecDocEntity.TypeDesc,
                matchEntity.TecDocEntity.Token_Type
            });

        public override Dictionary<string, dynamic> CreateMMIEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.MMIv8Entity.Manufacturer,
                matchEntity.MMIv8Entity.Model,
                matchEntity.MMIv8Entity.Mark_or_Series,
                matchEntity.MMIv8Entity.SubModel,
                matchEntity.MMIv8Entity.Token_Identifier
            });

        public override decimal CalculateScore()
        {
            return MatchBaseScoreExtensions.CalculateScoreText(
                [TecDocEntity[nameof(MatchEntity.TecDocEntity.TypeDesc)], TecDocEntity[nameof(MatchEntity.TecDocEntity.Token_Type)]],
                [MMIEntity[nameof(MatchEntity.MMIv8Entity.SubModel)], MMIEntity[nameof(MatchEntity.MMIv8Entity.Token_Identifier)]],
                DefaultScore);
        }
    }

    public class MatchEngineCode(IVersionProvider versionProvider, MatchEntity matchEntity) : MatchBase(versionProvider, matchEntity, MatchBaseType.EngineCode, MatchBaseMethod.Partial)
    {
        public override Dictionary<string, dynamic> CreateTecDocEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.TecDocEntity.LinkedEngineCodes
            });

        public override Dictionary<string, dynamic> CreateMMIEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.MMIv8Entity.Engine_Code
            });

        public override decimal CalculateScore() => MatchBaseScoreExtensions.CalculateScoreEngineMultiple(TecDocEntity[nameof(MatchEntity.TecDocEntity.LinkedEngineCodes)], MMIEntity[nameof(MatchEntity.MMIv8Entity.Engine_Code)]);
    }
    #endregion

    #region Automatic Matches
    public class MatchCC(IVersionProvider versionProvider, MatchEntity matchEntity) : MatchBase(versionProvider, matchEntity, MatchBaseType.CC, MatchBaseMethod.Automatic)
    {
        public override Dictionary<string, dynamic> CreateTecDocEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.TecDocEntity.CCTech
            });

        public override Dictionary<string, dynamic> CreateMMIEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.MMIv8Entity.Exact_CC
            });

        public override decimal CalculateScore() => MatchBaseScoreExtensions.CalculateScoreGradient(TecDocEntity[nameof(MatchEntity.TecDocEntity.CCTech)] - MMIEntity[nameof(MatchEntity.MMIv8Entity.Exact_CC)], 12, 0.5);
    }

    public class MatchCylinders(IVersionProvider versionProvider, MatchEntity matchEntity) : MatchBase(versionProvider, matchEntity, MatchBaseType.Cylinders, MatchBaseMethod.Automatic)
    {
        public override Dictionary<string, dynamic> CreateTecDocEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.TecDocEntity.Cyl
            });

        public override Dictionary<string, dynamic> CreateMMIEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.MMIv8Entity.Cylinders
            });

        public override decimal CalculateScore() => MatchBaseScoreExtensions.CalculateScoreGradient(TecDocEntity[nameof(MatchEntity.TecDocEntity.Cyl)] - MMIEntity[nameof(MatchEntity.MMIv8Entity.Cylinders)], 6);
    }

    public class MatchValves(IVersionProvider versionProvider, MatchEntity matchEntity) : MatchBase(versionProvider, matchEntity, MatchBaseType.Valves, MatchBaseMethod.Automatic)
    {
        public override Dictionary<string, dynamic> CreateTecDocEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.TecDocEntity.Calc_Valve
            });

        public override Dictionary<string, dynamic> CreateMMIEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.MMIv8Entity.Valve
            });

        public override decimal CalculateScore() => MatchBaseScoreExtensions.CalculateScoreGradient(TecDocEntity[nameof(MatchEntity.TecDocEntity.Calc_Valve)] - MMIEntity[nameof(MatchEntity.MMIv8Entity.Valve)], 8);
    }

    public class MatchBHP(IVersionProvider versionProvider, MatchEntity matchEntity) : MatchBase(versionProvider, matchEntity, MatchBaseType.BHP, MatchBaseMethod.Automatic, 0.95)
    {
        public override Dictionary<string, dynamic> CreateTecDocEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.TecDocEntity.Calc_BHP
            });

        public override Dictionary<string, dynamic> CreateMMIEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.MMIv8Entity.BHP
            });

        public override decimal CalculateScore()
            => TecDocEntity[ nameof(MatchEntity.TecDocEntity.Calc_BHP) ] == 0 || MMIEntity[ nameof(MatchEntity.MMIv8Entity.BHP) ] == 0
            ? Convert.ToDecimal(DefaultScore)
            : MatchBaseScoreExtensions.CalculateScoreGradient(TecDocEntity[nameof(MatchEntity.TecDocEntity.Calc_BHP)] - MMIEntity[nameof(MatchEntity.MMIv8Entity.BHP)], 12, 0.85);
    }

    public class MatchKW(IVersionProvider versionProvider, MatchEntity matchEntity) : MatchBase(versionProvider, matchEntity, MatchBaseType.KW, MatchBaseMethod.Automatic, 0.95)
    {
        public override Dictionary<string, dynamic> CreateTecDocEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.TecDocEntity.KW
            });

        public override Dictionary<string, dynamic> CreateMMIEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.MMIv8Entity.KW
            });

        public override decimal CalculateScore() 
            => TecDocEntity[ nameof(MatchEntity.TecDocEntity.KW) ] == 0 || MMIEntity[ nameof(MatchEntity.MMIv8Entity.KW) ] == 0 
            ? Convert.ToDecimal(DefaultScore) 
            : MatchBaseScoreExtensions.CalculateScoreGradient(TecDocEntity[nameof(MatchEntity.TecDocEntity.KW)] - MMIEntity[nameof(MatchEntity.MMIv8Entity.KW)], 15, 0.85);
    }

    public class MatchDate(IVersionProvider versionProvider, MatchEntity matchEntity) : MatchBase(versionProvider, matchEntity, MatchBaseType.Date, MatchBaseMethod.Automatic, 1)
    {
        public override Dictionary<string, dynamic> CreateTecDocEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                Start = matchEntity.TecDocEntity.DateRange.Start.ToString(),
                End = matchEntity.TecDocEntity.DateRange.End.ToString(),
                matchEntity.DateIntersection.date_TD_Coverage,
                matchEntity.DateIntersection.date_TD_Span,
            });

        public override Dictionary<string, dynamic> CreateMMIEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                Start = matchEntity.MMIv8Entity.DateRange.Start.ToString(),
                End = matchEntity.MMIv8Entity.DateRange.End.ToString(),
                matchEntity.DateIntersection.date_MMI_Coverage,
                matchEntity.DateIntersection.date_MMI_Span,
                matchEntity.DateIntersection.date_Intersection,
                matchEntity.DateIntersection.date_InverseIntersection,
            });

        public override decimal CalculateScore() => MatchBaseScoreExtensions.CalculateScoreDate3(TecDocEntity[ "date_TD_Coverage" ], TecDocEntity[ "date_TD_Span" ], MMIEntity[ "date_MMI_Coverage" ], MMIEntity[ "date_MMI_Span" ], MMIEntity[ "date_Intersection" ], MMIEntity[ "date_InverseIntersection" ]);
    }

    //public class MatchDateRemainder(IVersionProvider versionProvider, MatchEntity matchEntity) : MatchBase(versionProvider, matchEntity, MatchBaseType.DateRemainder, MatchBaseMethod.Automatic)
    //{
    //    public override Dictionary<string, dynamic> CreateTecDocEntity(MatchEntity matchEntity)
    //        => new();

    //    public override Dictionary<string, dynamic> CreateMMIEntity(MatchEntity matchEntity)
    //        => Globals.ObjToDictionary(new
    //        {
    //            matchEntity.DateIntersection.date_InverseIntersection
    //        });

    //    public override decimal CalculateScore() => ConvertScore(1.100 - (Convert.ToDouble(this.MMIEntity[ nameof(DateIntersection.date_InverseIntersection) ]) / 100), 0.01);
    //}

    //public class MatchDateCoveredMMI(IVersionProvider versionProvider, MatchEntity matchEntity) : MatchBase(versionProvider, matchEntity, MatchBaseType.DateCoveredMMI, MatchBaseMethod.Automatic)
    //{
    //    public override Dictionary<string, dynamic> CreateTecDocEntity(MatchEntity matchEntity)
    //        => new();

    //    public override Dictionary<string, dynamic> CreateMMIEntity(MatchEntity matchEntity)
    //        => Globals.ObjToDictionary(new
    //        {
    //            matchEntity.DateIntersection.date_MMI_Coverage
    //        });

    //    public override decimal CalculateScore() => ConvertScore(1.100 * (Convert.ToDouble(this.MMIEntity[ nameof(DateIntersection.date_MMI_Coverage) ]) / 100));
    //}

    //public class MatchDateCoveredTD(IVersionProvider versionProvider, MatchEntity matchEntity) : MatchBase(versionProvider, matchEntity, MatchBaseType.DateCoveredTD, MatchBaseMethod.Automatic)
    //{
    //    public override Dictionary<string, dynamic> CreateTecDocEntity(MatchEntity matchEntity)
    //        => Globals.ObjToDictionary(new
    //        {
    //            matchEntity.DateIntersection.date_TD_Coverage
    //        });

    //    public override Dictionary<string, dynamic> CreateMMIEntity(MatchEntity matchEntity)
    //        => new();

    //    public override decimal CalculateScore() => ConvertScore(1.100 * (Convert.ToDouble(this.TecDocEntity[ nameof(DateIntersection.date_TD_Coverage) ]) / 100));
    //}

    public class MatchRegion(IVersionProvider versionProvider, MatchEntity matchEntity) : MatchBase(versionProvider, matchEntity, MatchBaseType.Region, MatchBaseMethod.Automatic)
    {
        public override Dictionary<string, dynamic> CreateTecDocEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.TecDocEntity.Region
            });

        public override Dictionary<string, dynamic> CreateMMIEntity(MatchEntity matchEntity)
            => new();

        public override decimal CalculateScore() => TecDocEntity[nameof(MatchEntity.TecDocEntity.Region)] == "GB" ? (decimal)1.1 : (decimal)0.8;
    }

    #endregion

    public class MatchContext
    {
        [BsonElement]
        public string ContextId => GlobalHelpers.GenerateKey(new { TecDocEntity, MMIEntity });
        public Dictionary<string, dynamic> TecDocEntity { get; set; } = new();
        public Dictionary<string, dynamic> MMIEntity { get; set; } = new();
        public decimal ScoreOverride { get; set; }

        [JsonIgnore]
        [BsonIgnore]
        public FilterDefinition<MatchEntity> OverrideFilter 
            => Builders<MatchEntity>.Filter.And(
                [
                .. TecDocEntity is null ? [ Builders<MatchEntity>.Filter.Empty ] : TecDocEntity.Select(c => Builders<MatchEntity>.Filter.Eq($"TecDocEntity.{c.Key}", c.Value.ToString())),
                .. MMIEntity is null ? [Builders<MatchEntity>.Filter.Empty] : MMIEntity.Select(c => Builders<MatchEntity>.Filter.Eq($"MMIv8Entity.{c.Key}", c.Value.ToString()))
                ]
            );

        public MatchContext(object? tecdocEntityObj, object? mmiEntityObj, decimal scoreOverride)
        {
            TecDocEntity = tecdocEntityObj is null ? [] : GlobalHelpers.ObjToDictionary(tecdocEntityObj, false);
            MMIEntity = mmiEntityObj is null ? [] : GlobalHelpers.ObjToDictionary(mmiEntityObj, false);
            ScoreOverride = scoreOverride;
        }

        public MatchContext()
        {
        }

        public override string ToString()
        {
            string tecdocEntity = TecDocEntity.DictToString("; ");
            string mmiEntity = MMIEntity.DictToString("; ");
            return $"TD: [{tecdocEntity}] MMI: [{mmiEntity}] Score: {ScoreOverride}";
        }

    }

}
