using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MMIv8_Ktype.Models
{
    [JsonSerializable(typeof(SerializableResult<>))]
    public partial class ResultJsonContext : JsonSerializerContext
    {
    }
}
