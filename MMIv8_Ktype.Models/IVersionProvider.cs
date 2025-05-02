using MMIv8_Ktype.Models.Status;
using MongoDB.Bson;

namespace MMIv8_Ktype.Models
{
    public interface IVersionProvider
    {
        ObjectId VersionID { get; }

        StatusChange NewStatus(Status.Status status, string? detail = null);
    }
}
