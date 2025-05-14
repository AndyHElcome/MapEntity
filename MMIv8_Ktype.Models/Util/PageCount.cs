using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMIv8_Ktype.Models.Util
{
    public enum PageCount
    {
        /// <summary>
        /// Gives count of all documents in Collection
        /// </summary>
        EstimatedCount = 0,

        /// <summary>
        /// Gives count of filtered documents in Collection
        /// </summary>
        Count = 1,

        /// <summary>
        /// Count will return 0
        /// </summary>
        NoCount = 2,
    }
}
