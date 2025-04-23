using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MongoDB.Bson;
using System.Text.Json.Serialization;
using System.Text.Json;

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

    public class JsonObjectIdConverter : JsonConverter<ObjectId>
    {
        public override ObjectId Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) =>
                ObjectId.Parse(reader.GetString());

        public override void Write(
            Utf8JsonWriter writer,
            ObjectId objectId,
            JsonSerializerOptions options) =>
                writer.WriteStringValue(objectId.ToString());
    }
}
