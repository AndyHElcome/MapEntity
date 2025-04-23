using Serilog;

namespace MMIv8_Ktype.Api
{
    public interface IOperation
    {        
        Task ExecuteOperation(ILogger log);

        public static RefitClient GetRefitClient(ILogger log) => new(log);
    }
}
