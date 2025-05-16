using System.Text.Json.Serialization;
using System.Text.Json;
using System.Diagnostics;
using System.Reflection;

namespace MMIv8_Ktype.Models.Util
{
    public class JsonConverterFactoryForStackOfT : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
            => typeToConvert.IsGenericType
            && typeToConvert.GetGenericTypeDefinition() == typeof(Stack<>);

        public override JsonConverter CreateConverter(
            Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(typeToConvert.IsGenericType &&
                typeToConvert.GetGenericTypeDefinition() == typeof(Stack<>));

            Type elementType = typeToConvert.GetGenericArguments()[ 0 ];

            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(JsonConverterForStackOfT<>)
                    .MakeGenericType(new Type[] { elementType }),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: null,
                culture: null)!;

            return converter;
        }
    }
}
