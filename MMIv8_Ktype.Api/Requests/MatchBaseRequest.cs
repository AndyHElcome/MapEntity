using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Models.DateIntersection;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace MMIv8_Ktype.Api.Requests
{
    public record PutMatchBaseRequest(MatchBaseType MatchBaseType, string MatchHash, decimal NewScore) : IRequest;
    public record PutMatchBaseRequestWithContexts(MatchBaseType MatchBaseType, string MatchHash, decimal NewScore, string Contexts) : IRequest;
    public record DeleteMatchBaseRequest(MatchBaseType MatchBaseType, string MatchHash) : IRequest;
    public record AddMatchContext(TecDocEntity? TecDocEntity, MMIEntity? MMIEntity, decimal ScoreOverride) : IRequest;
    public record RemoveMatchContext(string? ContextId, TecDocEntity? TecDocEntity, MMIEntity? MMIEntity) : IRequest;
    public class MMIEntity
    {
        public string? Manufacturer { get; set; } = null;
        public string? Model { get; set; } = null;
        public string? SubModel { get; set; } = null;
        public string? Mark_or_Series { get; set; } = null;
        public string? Token_Identifier { get; set; } = null;
        public decimal? Engine_Size { get; set; } = null;
        public int? Cylinders { get; set; } = null;
        public string? Cylinder_Layout { get; set; } = null;
        public string? Cam { get; set; } = null;
        public int? Valve { get; set; } = null;
        public string? Body { get; set; } = null;
        public int? Doors { get; set; } = null;
        public string? Transmission { get; set; } = null;
        public int? Gears { get; set; } = null;
        public int? Exact_CC { get; set; } = null;
        public string? Drive { get; set; } = null;
        public string? Fuel { get; set; } = null;
        public int? BHP { get; set; } = null;
        public int? KW { get; set; } = null;
        public string? Engine_Code { get; set; } = null;
    }
    public class TecDocEntity
    {
        public int? KTypNr { get; set; } = null;
        public string? Make { get; set; }                 = null;
        public int? KModNr { get; set; }                  = null;
        public string? Model { get; set; }                = null;
        public string? Token_Model { get; set; }          = null;
        public string? Type { get; set; }                 = null;
        public string? Token_Type { get; set; }           = null;
        public int? KW { get; set; }                      = null;
        public int? PS { get; set; }                      = null;
        public int? Calc_BHP { get; set; }                = null;
        public decimal? Litre { get; set; }               = null;
        public int? Cyl { get; set; }                     = null;
        public int? Calc_Valve { get; set; }              = null;
        public string? Drive { get; set; }                = null;
        public string? FuelType { get; set; }             = null;
        public string? BodyType { get; set; }             = null;
        public int? CCTech { get; set; }                  = null;
        public string? SalesDesc { get; set; }            = null;
        public string? ModelGeneration { get; set; }      = null;
        public string? TypeDesc { get; set; }             = null;
        public int? Door { get; set; }                    = null;
        public string? Region { get; set; }               = null;
        public string? LinkedEngineCodes { get; set; } = null;
    }
}
