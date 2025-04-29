using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Status;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMIv8_Ktype.AccessMdb.Maps
{
    public class MatchMakeModelMaps
    {


    }

    public record MatchMakeModelRecord (ObjectId MatchID,
                                        string TD_SourceEntityModelHash,
                                        string TD_Make,
                                        string TD_Model,
                                        string MMI_SourceEntityModelHash,
                                        string MMI_Make,
                                        string MMI_Model,
                                        Status Status,
                                        string? Detail)
    {
        public static implicit operator MatchMakeModelRecord(MatchMakeModel matchMakeModel) 
            => new(matchMakeModel.MatchID,
                   matchMakeModel.TecDocModel.SourceEntityModelHash,
                   matchMakeModel.TecDocModel.Make,
                   matchMakeModel.TecDocModel.Model,
                   matchMakeModel.MMIv8Model.SourceEntityModelHash,
                   matchMakeModel.MMIv8Model.Make,
                   matchMakeModel.MMIv8Model.Model,
                   matchMakeModel.Status.Current.Status,
                   matchMakeModel.Status.Current.Detail);
    }
}
