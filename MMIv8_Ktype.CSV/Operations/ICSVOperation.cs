using MMIv8_Ktype.Models;

namespace MMIv8_Ktype.CSV.Operations
{
    internal interface ICSVOperation : IOperation
    {
        public string CSVPath { get; set; }
    }
}
