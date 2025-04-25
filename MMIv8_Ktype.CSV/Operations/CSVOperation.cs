using CsvHelper;
using MMIv8_Ktype.Api;
using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.CSV.Maps;
using MMIv8_Ktype.Models.Collections;
using Serilog;
using SharpCompress.Writers;
using System.Formats.Asn1;
using System.Globalization;
using System.Text;

namespace MMIv8_Ktype.CSV.Operations
{
    public abstract class CSVReadOperation(string csvPath) : ICSVOperation
    {
        public string CSVPath { get; set; } = csvPath;

        public CsvReadingStream CsvStream => new(CSVPath);

        public abstract Task ExecuteOperation(ILogger log);
    }

    public abstract class CSVWriteOperation(string csvPath, bool append = false) : ICSVOperation
    {
        public string CSVPath { get; set; } = csvPath;
        public bool Append { get; set; } = append;

        public CsvWritingStream CsvStream => new(CSVPath, Append);

        public abstract Task ExecuteOperation(ILogger log);
    }

    public class CsvWritingStream(string csvPath, bool append = false) : IDisposable
    {
        public StreamWriter Writer => new StreamWriter(csvPath, append, Encoding.UTF8);
        public CsvWriter CsvWriter => new CsvWriter(Writer, CultureInfo.InvariantCulture);

        public void Dispose()
        {
            Writer.Close();
            Writer.Dispose();
            CsvWriter.Dispose();
        }
    }

    public class CsvReadingStream(string csvPath) : IDisposable
    {
        public StreamReader Reader => new StreamReader(csvPath, Encoding.UTF8);
        public CsvReader CsvReader => new CsvReader(Reader, CultureInfo.InvariantCulture);

        public void Dispose()
        {
            Reader.Close();
            Reader.Dispose();
            CsvReader.Dispose();
        }
    }

    public class TestCsvReadOperation : CSVReadOperation
    {
        public TestCsvReadOperation(string csvPath) : base(csvPath) { }

        public async override Task ExecuteOperation(ILogger log)
        {
            List<PutMatchBaseRequest> updatedMatches = new();
            using (var csvReader = CsvStream.CsvReader)
            {
                updatedMatches = csvReader.GetRecords<PutMatchBaseRequest>().ToList();
            }

            log.Information("Read file: {CSVPath} ({count} records)", CSVPath, updatedMatches.Count);

            var matchBaseEndpoints = new RefitClient(log).CreateService<IMatchBaseEndpoints>();
            foreach (var match in updatedMatches)
            {
                await matchBaseEndpoints.UpdateMatchBaseScore(match);
            }
        }
    }

    public class TestCsvWriteOperation : CSVWriteOperation
    {
        public TestCsvWriteOperation(string csvPath, bool append) : base(csvPath, append) { }
        public TestCsvWriteOperation(string csvPath) : base(csvPath) { }

        public async override Task ExecuteOperation(ILogger log)
        {
            var matchMakeModels = await new RefitClient(log).CreateService<IMatchMakeModelEndpoints>().GenerateMakeModelMatch();

            using (var csvWriter = CsvStream.CsvWriter)
            {
                csvWriter.Context.RegisterClassMap<MatchMakeModelMap>();
                csvWriter.WriteRecords(matchMakeModels);
            }

            log.Information("Created file: {CSVPath} ({count} records)", CSVPath, matchMakeModels.Count);
        }
    }

    public class GenerateMakeModelMatch : CSVWriteOperation
    {
        public GenerateMakeModelMatch(string csvPath, bool append) : base(csvPath, append) { }
        public GenerateMakeModelMatch(string csvPath) : base(csvPath) { }

        public async override Task ExecuteOperation(ILogger log)
        {
            var matchMakeModels = await new RefitClient(log).CreateService<IMatchMakeModelEndpoints>().GenerateMakeModelMatch();

            using (var csvWriter = CsvStream.CsvWriter)
            {
                csvWriter.Context.RegisterClassMap<MatchMakeModelMap>();
                csvWriter.WriteRecords(matchMakeModels);
            }

            log.Information("Created file: {CSVPath} ({count} records)", CSVPath, matchMakeModels.Count);
        }
    }
}
