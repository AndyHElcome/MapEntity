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
            target.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            target.NumberHandling = JsonNumberHandling.AllowReadingFromString;
            target.PropertyNameCaseInsensitive = true;
            target.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            target.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
            target.WriteIndented = true;
        }

        [Obsolete()]
        public static void ApplyJsonOptions(JsonSerializerOptions target, JsonSerializerOptions source)
        {
            foreach (var converter in source.Converters)
                target.Converters.Add(converter);

            target.DefaultIgnoreCondition = source.DefaultIgnoreCondition;
            target.NumberHandling = source.NumberHandling;
            target.PropertyNameCaseInsensitive = source.PropertyNameCaseInsensitive;
            target.PropertyNamingPolicy = source.PropertyNamingPolicy;
            target.TypeInfoResolver = source.TypeInfoResolver;
            target.WriteIndented = source.WriteIndented;
        }
    }
}
