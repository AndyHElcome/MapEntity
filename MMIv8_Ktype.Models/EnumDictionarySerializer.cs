
using MMIv8_Ktype.Models.Collections;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using System.Collections;

namespace MMIv8_Ktype.Models
{
    public class EnumDictionarySerializer<TKey, TDictionary> : DictionarySerializerBase<TDictionary>
        where TKey : struct, Enum
        where TDictionary : class, IDictionary, new()
    {
        public EnumDictionarySerializer() 
            : base(DictionaryRepresentation.Document, 
                   new EnumSerializer<TKey>(BsonType.String),
                    new ObjectSerializer(type => ObjectSerializer.DefaultAllowedTypes(type) 
                                              || type == typeof(MatchBase) 
                                              || type.IsSubclassOf(typeof(MatchBase))))
        {
        }

        protected override TDictionary CreateInstance()
        {
            return new TDictionary();
        }
    }

    

    //public class DateOnlySerializer : StructSerializerBase<DateOnly>
    //{
    //    private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1);

    //    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateOnly value)
    //    {
    //        var dateTime = value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    //        context.Writer.WriteDateTime(BsonUtils.ToMillisecondsSinceEpoch(dateTime));
    //    }

    //    public override DateOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    //    {
    //        var dateTime = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(context.Reader.ReadDateTime());
    //        return DateOnly.FromDateTime(dateTime);
    //    }
    //}
}
