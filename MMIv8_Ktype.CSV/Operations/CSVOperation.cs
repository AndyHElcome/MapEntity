using CsvHelper;
using MMIv8_Ktype.Api;
using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.CSV.Maps;
using MMIv8_Ktype.Models.Collections;
using Serilog;
using System.Globalization;
using System.Text;

namespace MMIv8_Ktype.CSV.Operations
{
    public abstract class CSVReadOperation(string csvPath) : ICSVOperation
    {
        public string CSVPath { get; set; } = csvPath;

        public abstract Task ExecuteOperation(ILogger log);
        internal CsvReader GetCsvReader()
        {
            using var reader = new StreamReader(CSVPath, Encoding.UTF8);
            return new CsvReader(reader, CultureInfo.InvariantCulture);
        }
    }

    public abstract class CSVWriteOperation(string csvPath) : ICSVOperation
    {
        public string CSVPath { get; set; } = csvPath;

        public abstract Task ExecuteOperation(ILogger log);
        internal CsvWriter GetCsvWriter(bool Append = false)
        {
            using var writer = new StreamWriter(CSVPath, Append, Encoding.UTF8);
            return new CsvWriter(writer, CultureInfo.InvariantCulture);
        }

    }

    public class TestCsvReadOperation(string csvPath) : CSVReadOperation(csvPath)
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            var matchBaseEndpoints = new RefitClient(log).CreateService<IMatchBaseEndpoints>();

            List<PutMatchBaseRequest> updatedMatches = new();

            using (CsvReader csvReader = this.GetCsvReader())
            {
                updatedMatches = csvReader.GetRecords<PutMatchBaseRequest>().ToList();
            }

            foreach (var match in updatedMatches)
            {
                await matchBaseEndpoints.UpdateMatchBaseScore(match);
            }

        }
    }


    public class TestCsvWriteOperation(string csvPath) : CSVWriteOperation(csvPath)
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            var matchMakeModels = await new RefitClient(log).CreateService<IMatchMakeModelEndpoints>().GenerateModelMatch();

            using (CsvWriter csvWriter = this.GetCsvWriter())
            {
                csvWriter.Context.RegisterClassMap<MatchMakeModelMap>();
                csvWriter.WriteRecords(matchMakeModels);
            }
        }
    }

    public class GenerateModelMatchCSV(string csvPath) : CSVWriteOperation(csvPath)
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            var matchMakeModelEndpoints = new RefitClient(log).CreateService<IMatchMakeModelEndpoints>();

            List<MatchMakeModel>? matchMakeModels = await matchMakeModelEndpoints.GenerateModelMatch();

            using (CsvWriter csvWriter = this.GetCsvWriter())
            {
                csvWriter.Context.RegisterClassMap<MatchMakeModelMap>();
                csvWriter.WriteRecords(matchMakeModels);
            }
        }
    }
}
