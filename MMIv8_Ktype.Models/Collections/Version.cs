using CsvHelper.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MMIv8_Ktype.Models.Collections
{
    public class Version
    {
        [BsonId]
        [CsvHelper.Configuration.Attributes.Ignore]
        public ObjectId VersionID { get; set; } = ObjectId.GenerateNewId();
        public int VersionNumber { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string TecDocEntityVersion { get; set; }
        public string MMIv8EntityVersion { get; set; }
        public User User { get; set; }
    }

    public sealed class VersionMap : ClassMap<Version>
    {
        public VersionMap()
        {
            Map(m => m.VersionNumber);
            Map(m => m.TecDocEntityVersion);
            Map(m => m.MMIv8EntityVersion);
            References<UserMap>(m => m.User);
        }
    }

    public class CreateBaseVersion
    {
        [Required]
        public string TecDocEntityVersion { get; set; }

        [Required]
        public string MMIv8EntityVersion { get; set; }

        [Required]
        public string User { get; set; }
    }
}
