using MMIv8_Ktype.Models.Requests;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMIv8_Ktype.Models.Abstractions
{
    public interface IMappingService
    {
        Task<ObjectId> CreateMakeModelMatch(MakeModelMatchRequest matchMakeModel);
    }
}
