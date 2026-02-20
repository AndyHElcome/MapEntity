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
        public new SortDefinition<MatchEntity>? GetSort()
        {
            return base.GetSort();
        }
    }

    public class MatchEntityFilterRequest : FilterQuery<MatchEntity>
    {
        public string? MakeModelMatchId { get; set; }
        public string? TecDocEntityId { get; set; }
        public string? MMIv8EntityId { get; set; }
        public bool? IsCheck { get; set; }
        public bool? IsMatched { get; set; }
        public bool? IsFailed { get; set; }
        public bool? HasDifference { get; set; }
        public Status[]? Status { get; set; }

        public FilterDefinition<MatchEntity> GetFilter()
        {
            var filterBuilder = Builders<MatchEntity>.Filter;
            var filter = filterBuilder.Empty;

            if (MakeModelMatchId is not null)
                filter = filter & filterBuilder.Eq(c => c.MatchMakeModelMatchID, ObjectId.Parse(MakeModelMatchId));

            if (TecDocEntityId is not null)
                filter = filter & filterBuilder.Eq(c => c.TecDocEntity.DocumentId, ObjectId.Parse(TecDocEntityId));

            if (MMIv8EntityId is not null)
                filter = filter & filterBuilder.Eq(c => c.MMIv8Entity.DocumentId, ObjectId.Parse(MMIv8EntityId));

            if (IsCheck is not null)
                filter = filter & filterBuilder.Eq(c => c.MatchRefine.IsCheck, IsCheck);

            if (IsMatched is not null)
                filter = filter & filterBuilder.Eq(c => c.Matched, IsMatched);

            if (IsFailed is not null)
                filter = filter & filterBuilder.Eq(c => c.MatchResult.Failed, IsFailed);

            if (HasDifference is not null)
                filter = filter & filterBuilder.Eq(c => c.MatchRefine.Difference, HasDifference);

            if (Status is not null && Status.Length != 0)
                filter = filter & filterBuilder.In(c => c.Status.Current.Status, Status);

            return filter;
        }
    }
}
