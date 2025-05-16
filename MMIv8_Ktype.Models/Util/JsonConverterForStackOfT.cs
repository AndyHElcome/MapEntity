using System.Text.Json.Serialization;
using System.Text.Json;

namespace MMIv8_Ktype.Models.Util
{
    public class JsonConverterForStackOfT<T> : JsonConverter<Stack<T>>
    {
        public override Stack<T> Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }
            reader.Read();

            var elements = new Stack<T>();

            while (reader.TokenType != JsonTokenType.EndArray)
            {
                elements.Push(JsonSerializer.Deserialize<T>(ref reader, options)!);

                reader.Read();
            }

            return elements;
        }

        public override void Write(
            Utf8JsonWriter writer, Stack<T> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();

            var reversed = new Stack<T>(value);

            foreach (T item in reversed)
            {
                JsonSerializer.Serialize(writer, item, options);
            }

            writer.WriteEndArray();
        }
    }
}
