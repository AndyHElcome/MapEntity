using MongoDB.Bson;

namespace MMIv8_Ktype.Models.Status
{
    public class StatusChange
    {
        public Status Status { get; set; }
        public DateTime DateOfChange { get; set; }
        public ObjectId VersionID { get; set; }
        public string? Detail { get; set; }

        public override string ToString()
        {
            return Status.ToString();
        }
        public string GridFormat()
        {
            if (Detail is not null && Detail != string.Empty)
                return $"{Status} - {DateOfChange.ToShortDateString()} - ({Detail})";
            else
                return $"{Status} - {DateOfChange.ToShortDateString()}";
        }
    }
}
