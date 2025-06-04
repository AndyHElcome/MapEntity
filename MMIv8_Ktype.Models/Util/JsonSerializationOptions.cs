using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Serialization;

namespace MMIv8_Ktype.Models.Util
{
    public static class JsonSerializationOptions
    {
        public static JsonSerializerOptions GetJsonSerializerOptions(this JsonSerializerOptions jsonSerializerOptions)
        {
            ApplyJsonSettings(jsonSerializerOptions);
            return jsonSerializerOptions;
        }

        public static void ApplyJsonSettings(JsonSerializerOptions target)
        {
            target.Converters.Add(new JsonStringEnumConverter());
            target.Converters.Add(new JsonObjectIdConverter());
            target.Converters.Add(new JsonConverterFactoryForStackOfT());
            target.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            target.NumberHandling = JsonNumberHandling.AllowReadingFromString;
            target.PropertyNameCaseInsensitive = true;
            target.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            target.TypeInfoResolver = JsonTypeInfoResolver.Combine(
                ResultJsonContext.Default,
                new DefaultJsonTypeInfoResolver()
            );
            target.WriteIndented = true;
        }
    }
}
