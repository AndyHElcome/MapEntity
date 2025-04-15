using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MongoDB.Bson;

namespace MMIv8_Ktype.Models
{
    public class ObjectIdConverter : DefaultTypeConverter
    {
        public override string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        {
            if (value == null || value.GetType() != typeof(ObjectId))
                return string.Empty;

            var objectId = (ObjectId)value;

            return objectId.ToString() ?? string.Empty;
        }
    }
}
