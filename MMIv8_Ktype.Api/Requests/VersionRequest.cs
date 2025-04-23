namespace MMIv8_Ktype.Api.Requests
{
    public record CreateVersionRequest(string TecDocEntityVersion, string MMIv8EntityVersion, string User) : IRequest;
}
