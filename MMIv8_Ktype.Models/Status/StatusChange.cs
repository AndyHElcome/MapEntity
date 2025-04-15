using CsvHelper.Configuration;
using MMIv8_Ktype.Models.Collections;

namespace MMIv8_Ktype.Models.Status
{
    public class StatusChange
    {
        public Status Status { get; set; }
        public DateTime DateOfChange { get; set; }
        public Collections.Version Version { get; set; }
        public string? Detail { get; set; }

        public override string ToString()
        {
            return Status.ToString();
        }
    }

    public sealed class StatusChangeMap : ClassMap<StatusChange>
    {
        public StatusChangeMap()
        {
            Map(m => m.Status);
            Map(m => m.DateOfChange);

            References<VersionMap>(m => m.Version);
        }
    }
}
