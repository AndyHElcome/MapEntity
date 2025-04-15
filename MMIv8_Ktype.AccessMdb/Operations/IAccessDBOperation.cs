using MMIv8_Ktype.Models;

namespace MMIv8_Ktype.AccessMdb.Operations
{
    internal interface IAccessDBOperation : IOperation
    {
        public string DBPath { get; set; }
        public string TableName { get; set; }
    }
}
