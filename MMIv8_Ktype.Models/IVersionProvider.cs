using MMIv8_Ktype.Models.Status;

namespace MMIv8_Ktype.Models
{
    public interface IVersionProvider
    {
        Collections.Version Version { get; }

        StatusChange NewStatus(Status.Status status, string? detail = null);
    }
}
