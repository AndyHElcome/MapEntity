using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMIv8_Ktype.Api.Responses
{

    public class UpdateResultDTO
    {
        public long MatchedCount { get; set; }
        public long ModifiedCount { get; set; }
        public bool IsAcknowledged { get; set; }


        public static implicit operator UpdateResultDTO(UpdateResult result)
        {
            if (result == null)
                return new UpdateResultDTO
                {
                    IsAcknowledged = false
                };

            return new UpdateResultDTO
            {
                MatchedCount = result.MatchedCount,
                ModifiedCount = result.ModifiedCount,
                IsAcknowledged = result.IsAcknowledged
            };
        }
    }
}
