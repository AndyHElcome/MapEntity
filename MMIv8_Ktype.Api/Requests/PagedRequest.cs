using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel;
using System.Security.Cryptography;

namespace MMIv8_Ktype.Api.Requests
{
    public class PagedRequest(int? page = 1, int? pageSize = 25) : IRequest
    {
        public int? Page { get; set; } = page;
        public int? PageSize { get; set; } = pageSize;
    }

    public class PagedCursorRequest<Tid>(string? cursor = null, int? pageSize = 25) : IRequest
    {
        public string? Cursor { get; set; } = cursor;
        public int? PageSize { get; set; } = pageSize;

        public Tid? GetCursor()        
        {
            if (Cursor == null)
                return default;

            if (typeof(Tid) == typeof(ObjectId))
            {
                var objectId = ObjectId.TryParse(Cursor, out var objectid) ? objectid : ObjectId.Empty;

                return (Tid?)(object)objectId;
            }

            if (typeof(Tid) == typeof(string))
            {
                return (Tid?)(object)Cursor.ToString();
            }

            throw new NotSupportedException($"Cannot get cursor from Type {nameof(Tid)}");
        }
    }
}
