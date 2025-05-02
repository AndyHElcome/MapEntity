using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Dynamic;

namespace MMIv8_Ktype.Models.Collections
{
    [BsonDiscriminator(Required = true)]
    [BsonKnownTypes(typeof(MatchModel), typeof(MatchMark), typeof(MatchIdentifier), typeof(MatchEngineCode),
                    typeof(MatchBody), typeof(MatchDrive), typeof(MatchFuel),
                    typeof(MatchCC), typeof(MatchCylinders), typeof(MatchValves), typeof(MatchBHP), typeof(MatchKW), typeof(MatchRegion), typeof(MatchDate))]
    public abstract class MatchBase : ICollectionEntity<string>, IStatusHistory
    {
        [BsonId]
        public string MatchHash { get; set; }
        public MatchBaseType MatchBaseType;
        public MatchBaseMethod MatchBaseMethod;
        public double DefaultScore;
        public Dictionary<string, dynamic> TecDocEntity = new();
        public Dictionary<string, dynamic> MMIEntity = new();

        [BsonDefaultValue(null)]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Score { get; set; }

        [BsonIgnoreIfDefault]
        public List<MatchContext>? MatchContexts { get; set; }

        public StatusHistory Status { get; set; }

        [BsonIgnore]
        public string DocumentId => MatchHash;

        public MatchBase(IVersionProvider versionProvider, MatchEntity matchEntity, MatchBaseType matchBaseType, MatchBaseMethod matchBaseMethod, double defaultScore = 1.1)
        {
            Status = new(versionProvider);
            MatchBaseType = matchBaseType;
            MatchBaseMethod = matchBaseMethod;
            DefaultScore = defaultScore;

            TecDocEntity = CreateTecDocEntity(matchEntity);
            MMIEntity = CreateMMIEntity(matchEntity);
            MatchHash = GenerateMatchKey();
            Score = CalculateScore();
        }

        [Obsolete("VersionProvider Required")]
        public MatchBase(MatchEntity matchEntity, MatchBaseType matchBaseType, MatchBaseMethod matchBaseMethod, double defaultScore = 1.1)
        {
            throw new NotImplementedException("VersionProvider is now required");
        }

        public abstract decimal CalculateScore();
        public abstract Dictionary<string, dynamic> CreateTecDocEntity(MatchEntity matchEntity);
        public abstract Dictionary<string, dynamic> CreateMMIEntity(MatchEntity matchEntity);

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
            string tecdocEntity = string.Join(' ', TecDocEntity.Select(c => c.Value.ToString()).Where(c => !string.IsNullOrWhiteSpace(c)));
            string mmiEntity = string.Join(' ', MMIEntity.Select(c => c.Value.ToString()).Where(c => !string.IsNullOrWhiteSpace(c)));
            return $"MatchBaseType {MatchBaseType} Score {Score} TD [{tecdocEntity}] MMI [{mmiEntity}] MatchHash {MatchHash}";
        }

        public dynamic BuildCsvObject()
        {
            dynamic csvObj = new ExpandoObject();

            csvObj.MatchHash = MatchHash;
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

            return csvObj;
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

        public override decimal CalculateScore() => MatchBaseScoreExtensions.CalculateScoreGradient(TecDocEntity[nameof(MatchEntity.TecDocEntity.Calc_BHP)] - MMIEntity[nameof(MatchEntity.MMIv8Entity.BHP)], 12, 0.85);
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

        public override decimal CalculateScore() => MatchBaseScoreExtensions.CalculateScoreGradient(TecDocEntity[nameof(MatchEntity.TecDocEntity.KW)] - MMIEntity[nameof(MatchEntity.MMIv8Entity.KW)], 15, 0.85);
    }

    public class MatchDate(IVersionProvider versionProvider, MatchEntity matchEntity) : MatchBase(versionProvider, matchEntity, MatchBaseType.Date, MatchBaseMethod.Automatic, 1)
    {
        public override Dictionary<string, dynamic> CreateTecDocEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.TecDocEntity.DateRange.Start,
                matchEntity.TecDocEntity.DateRange.End,
                matchEntity.DateIntersection.date_TD_Coverage,
            });

        public override Dictionary<string, dynamic> CreateMMIEntity(MatchEntity matchEntity)
            => GlobalHelpers.ObjToDictionary(new
            {
                matchEntity.MMIv8Entity.DateRange.Start,
                matchEntity.MMIv8Entity.DateRange.End,
                matchEntity.DateIntersection.date_MMI_Coverage,
                matchEntity.DateIntersection.date_InverseIntersection,
            });

        public override decimal CalculateScore() => MatchBaseScoreExtensions.CalculateScoreDate2(TecDocEntity["date_TD_Coverage"], MMIEntity["date_MMI_Coverage"], MMIEntity["date_InverseIntersection"]);
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

    [Obsolete("Use Record")]
    public class UpdateMatchBase
    {
        public string MatchHash { get; set; }
        public MatchBaseType MatchBaseType { get; set; }
        public decimal Score { get; set; }
    }

    public class MatchContext
    {
        public Dictionary<string, dynamic> TecDocEntity { get; set; }
        public Dictionary<string, dynamic> MMIEntity { get; set; }
        public IEnumerable<FilterDefinition<MatchEntity>> OverrideFilter => (List<FilterDefinition<MatchEntity>>)[.. TecDocEntity.Select(c => Builders<MatchEntity>.Filter.Eq($"TecDocEntity.{c.Key}", c.Value.ToString())), .. MMIEntity.Select(c => Builders<MatchEntity>.Filter.Eq($"MMIv8Entity.{c.Key}", c.Value.ToString()))];
        public decimal ScoreOverride { get; set; }
    }
}
