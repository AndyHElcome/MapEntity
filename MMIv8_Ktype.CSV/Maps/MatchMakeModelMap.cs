using CsvHelper.Configuration;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.CSV.Converters;
using MMIv8_Ktype.Models.Collections;
using System.Globalization;

namespace MMIv8_Ktype.CSV.Maps
{
    public sealed class MatchMakeModelMap : ClassMap<MatchMakeModel>
    {
        public MatchMakeModelMap()
        {
            Map(m => m.DocumentId).TypeConverter<ObjectIdConverter>().Name(nameof(MatchMakeModel.DocumentId));
            References<MongoSourceEntityModelMap>(m => m.TecDocModel).Prefix("TD_");
            References<MongoSourceEntityModelMap>(m => m.MMIv8Model).Prefix("MMI_");

            References<StatusHistoryMap>(m => m.Status);
        }
    }
    public sealed class MatchMakeModelRequestMap : ClassMap<MatchMakeModelRequest>
    {
        public MatchMakeModelRequestMap()
        {
            Parameter(nameof(MatchMakeModelRequest.TD_SourceEntityModelHash)).Name("TD_DocumentId");
            Parameter(nameof(MatchMakeModelRequest.MMI_SourceEntityModelHash)).Name("MMI_DocumentId");
        }
    }
}
