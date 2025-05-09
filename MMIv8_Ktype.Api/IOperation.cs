using Serilog;

namespace MMIv8_Ktype.Api
{
    public interface IOperation
    {        
        Task ExecuteOperation(ILogger log);
    }
}
