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
    public record MatchRefineRecord(ObjectId DocumentId,
                                    ObjectId MatchMakeModelMatchID,
                                    int KTypNr,
                                    int MMI_V8_Key,
                                    Status Status,
                                    string? Detail

        )
    {
        public static implicit operator MatchRefineRecord(MatchEntity matchEntity)
            => new(matchEntity.DocumentId,
                   matchEntity.MatchMakeModelMatchID,
                   matchEntity.TecDocEntity.KTypNr,
                   matchEntity.MMIv8Entity.MMI_V8_Key,
                   matchEntity.Status.Current.Status,
                   matchEntity.Status.Current.Detail


                );
    }
}
