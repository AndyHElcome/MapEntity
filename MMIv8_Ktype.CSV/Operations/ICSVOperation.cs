using MMIv8_Ktype.Api;

namespace MMIv8_Ktype.CSV.Operations
{
    internal interface ICSVOperation : IOperation
    {
        string CSVPath { get; set; }
    }
}
