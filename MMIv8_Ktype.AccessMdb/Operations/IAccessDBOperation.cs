using MMIv8_Ktype.Api;

namespace MMIv8_Ktype.AccessMdb.Operations
{
    internal interface IAccessDBOperation : IOperation
    {
        string DBPath { get; set; }
        string TableName { get; set; }
    }
}
