using System.Reflection;
using MongoDB.Bson.Serialization;
using MMIv8_Ktype.Models.Status;

namespace MMIv8_Ktype.Core.Helpers
{
    public static class Globals
    {
        public static List<(PropertyInfo propertyInfo, BsonMemberMap? bsonMemberMap)> GetDifferences<T>(this T originalObj, T newObj)
            where T : Models.IUpdateDifferences
        {
            if (originalObj == null || newObj == null)
                return [];

            var bsonMemberMaps = BsonClassMap.LookupClassMap(typeof(T)).AllMemberMaps;

            List<(PropertyInfo, BsonMemberMap?)> differences = new();
            foreach (PropertyInfo property in originalObj.GetType().GetProperties())
            {
                object value1 = property.GetValue(originalObj, null) ?? new { };
                object value2 = property.GetValue(newObj, null) ?? new { };
                if (!value1.Equals(value2))
                {
                    BsonMemberMap? bsonMemberMap = bsonMemberMaps.Where(c => c.MemberName == property.Name).FirstOrDefault();
                    differences.Add((property, bsonMemberMap));
                }
            }
            return differences;
        }
    }
}
