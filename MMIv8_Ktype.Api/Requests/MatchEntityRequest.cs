using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Status;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MMIv8_Ktype.Api.Requests
{
    public record MatchEntityByExternalRequest(int KtypNr, int MMI_V8_Key) : IRequest;
    public record UpdateFlagRequest(int KTypNr, int MMI_V8_Key, bool Flag, string? Detail = null) : IRequest;
    public record MMI_V8_Key(int ExternalId) : IRequest;



    public class MatchEntitySortRequest : SortQuery<MatchEntity>
    {
        public SortDirection? ScoreSum { get; set; }

        public override SortDefinition<MatchEntity> GetSort()
        {
            var sortBuilder = Builders<MatchEntity>.Sort;
            var sort = sortBuilder.Combine();

            if (ScoreSum is not null)
                if (ScoreSum == SortDirection.Ascending)
                    sort = sortBuilder.Combine(sort, sortBuilder.Ascending(c => c.ScoreSum));
                else
                    sort = sortBuilder.Combine(sort, sortBuilder.Descending(c => c.ScoreSum));

            return sort;
        }
    }

    public class MatchEntityFilterRequest : FilterQuery<MatchEntity>
    {
        public string? MakeModelMatchId { get; set; }
        public string? TecDocEntityId { get; set; }
        public string? MMIv8EntityId { get; set; }
        public bool? IsMatched { get; set; }
        public bool? IsFailed { get; set; }
        public Status[]? Status { get; set; }

        public override FilterDefinition<MatchEntity> GetFilter()
        {
            var filterBuilder = Builders<MatchEntity>.Filter;
            var filter = filterBuilder.Empty;

            if (MakeModelMatchId is not null)
                filter = filter & filterBuilder.Eq(c => c.MatchMakeModelMatchID, ObjectId.Parse(MakeModelMatchId));

            if (TecDocEntityId is not null)
                filter = filter & filterBuilder.Eq(c => c.TecDocEntity.DocumentId, ObjectId.Parse(TecDocEntityId));

            if (MMIv8EntityId is not null)
                filter = filter & filterBuilder.Eq(c => c.MMIv8Entity.DocumentId, ObjectId.Parse(MMIv8EntityId));

            if (IsMatched is not null)
                filter = filter & filterBuilder.Eq(c => c.Matched, IsMatched);

            if (IsFailed is not null)
                filter = filter & filterBuilder.Eq(c => c.MatchResult.Failed, IsFailed);

            if (Status is not null && Status.Length != 0)
                filter = filter & filterBuilder.In(c => c.Status.Current.Status, Status);

            return filter;
        }
    }

}
