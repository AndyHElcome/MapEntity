namespace MMIv8_Ktype.Models
{
    public interface IOperation
    {
        public Task ExecuteOperation(Serilog.ILogger Log);
    }
}
