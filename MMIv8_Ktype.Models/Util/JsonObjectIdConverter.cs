using MongoDB.Bson;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace MMIv8_Ktype.Models.Util
{
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
