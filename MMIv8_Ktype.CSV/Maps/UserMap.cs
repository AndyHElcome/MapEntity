using CsvHelper.Configuration;
using MMIv8_Ktype.Models.Collections;

namespace MMIv8_Ktype.CSV.Maps
{
    public sealed class UserMap : ClassMap<User>
    {
        public UserMap()
        {
            Map(m => m.DocumentId).Ignore();
            Map(m => m.Name);
        }
    }
}
