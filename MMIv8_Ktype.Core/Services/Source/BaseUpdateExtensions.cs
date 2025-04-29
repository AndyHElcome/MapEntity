using MMIv8_Ktype.Core.Helpers;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Attributes;
using MongoDB.Driver;

namespace MMIv8_Ktype.Core.Services.Source
{
    public static class BaseUpdateExtensions
    {
        public static UpdateDefinition<T> UpdateDifferences<T>(this UpdateDefinition<T> update, T currentSourceTecDocPC, T newSourceTecDocPC, out string? Differences)
            where T : IUpdateDifferences
        {
            var differences = currentSourceTecDocPC.GetDifferences(newSourceTecDocPC);
            Differences = null;

            foreach (var (propertyInfo, bsonMemberMap) in differences)
            {
                if (bsonMemberMap is not null
                    && bsonMemberMap.ClassMap.IdMemberMap != bsonMemberMap
                    && !propertyInfo.CustomAttributes.Any(c => c.AttributeType == typeof(DoNotUpdateDifferences)))
                {
                    update = update.Set(bsonMemberMap.MemberName, propertyInfo.GetValue(newSourceTecDocPC));

                    Differences = Differences is null ? propertyInfo.Name : $"{Differences}, {propertyInfo.Name}";
                }
            }

            return update;
        }
    }
}
