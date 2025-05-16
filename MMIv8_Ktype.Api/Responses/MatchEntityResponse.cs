using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Models.Status;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMIv8_Ktype.Api.Responses
{
    public record MatchEntityBackup(int MMI_V8_Key, int KTypNr, bool Matched, string MatchDetail, bool Failed, string FailDetail, Status Status) : IResponse;

    public record MatchEntitySummary(ObjectId DocumentId, ObjectId MatchMakeModelMatchID, ObjectId TecDocEntityId, ObjectId MMIv8EntityId, bool Failed, string? FailDetail, bool PreviousMatch, bool IsCheck, bool Difference, decimal? BestScore, decimal? ScoreSum, bool IsBest, bool Matched, string? MatchDetail, Status Status);
}
