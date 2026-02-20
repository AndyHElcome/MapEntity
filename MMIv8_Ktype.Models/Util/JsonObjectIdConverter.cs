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
    public sealed class NullableObjectIdJsonConverter : JsonConverter<ObjectId?>
    {
        public override ObjectId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.String &&
                ObjectId.TryParse(reader.GetString(), out var oid))
                return oid;

            return null; // or throw if you prefer strictness
        }

        public override void Write(Utf8JsonWriter writer, ObjectId? value, JsonSerializerOptions options)
        {
            if (value is null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(value.Value.ToString());
        }
    }

}
