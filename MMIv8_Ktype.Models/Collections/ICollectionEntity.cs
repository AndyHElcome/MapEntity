using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MMIv8_Ktype.Models.Collections
{
    public interface ICollectionEntity<Tid>
    {
        Tid DocumentId { get; }
    }
}
