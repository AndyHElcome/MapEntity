using CsvHelper;
using MMIv8_Ktype.Api;
using MMIv8_Ktype.Models.Collections;
using System.Globalization;
using System.Text;

namespace MMIv8_Ktype.CSV.Operations
{
    public abstract class CSVReadOperation(string csvPath) : ICSVOperation
    {
        public string CSVPath { get; set; } = csvPath;

        public abstract Task ExecuteOperation(Serilog.ILogger Log);
    }

    public abstract class CSVWriteOperation(string csvPath) : ICSVOperation
    {
        public string CSVPath { get; set; } = csvPath;
        internal CsvWriter GetCsvWriter()
        {
            using var writer = new StreamWriter(CSVPath, false, Encoding.UTF8);
            return new CsvWriter(writer, CultureInfo.InvariantCulture);
        }

        public abstract Task ExecuteOperation(Serilog.ILogger Log);
    }

    public class GenerateModelMatchCSV(string csvPath, MMIv8_KtypeService mmiv8_KtypeService) : CSVWriteOperation(csvPath)
    {
        public async override Task ExecuteOperation(Serilog.ILogger Log)
        {

            List<MatchMakeModel>? matchMakeModels = await mmiv8_KtypeService.GenerateModelMatch();

            using (CsvWriter csvWriter = this.GetCsvWriter())
            {
                csvWriter.Context.RegisterClassMap<MatchMakeModelMap>();
                csvWriter.WriteRecords(matchMakeModels);
            }
        }
    }
}
