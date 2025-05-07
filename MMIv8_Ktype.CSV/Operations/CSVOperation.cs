using CsvHelper;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MMIv8_Ktype.Api;
using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.CSV.Maps;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Util;
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

    public sealed class CSVBackupOperation(string csvPath) : ICSVOperation
    {
        public string CSVPath { get; set; } = csvPath;

        public async Task ExecuteOperation(ILogger log)
        {
            var path = Path.GetFullPath(CSVPath);
            var client = new RefitClient(log);

            log.Information("Starting Backup into {path}", path);

            var matchMakeModel = client.CreateService<IMatchMakeModelEndpoints>();
            var filepath = Path.Combine(path, "MatchMakeModel.csv");
            using (var csvWriter = new CsvWritingStream(filepath).CsvWriter)
            {
                csvWriter.Context.RegisterClassMap<MatchMakeModelMap>();
                csvWriter.WriteRecords(await matchMakeModel.GenerateMakeModelMatch());
            }
            log.Information("Created file for Match Make Model {path}", filepath);


            var matchBase = client.CreateService<IMatchBaseEndpoints>();
            foreach (var matchBaseType in (MatchBaseType[])Enum.GetValues(typeof(MatchBaseType)))
            {
                filepath = Path.Combine(path, $"Match{matchBaseType.ToString()}.csv");
                var matchBases = await matchBase.GetByMatchBaseType(matchBaseType);

                using (var csvWriter = new CsvWritingStream(filepath).CsvWriter)
                {
                    csvWriter.WriteRecords(matchBases.Select(c => c.BuildCsvObject()));
                }

                log.Information("Created file for Match {matchBaseType} {path}", matchBaseType.ToString(), filepath);
            }


            var matchEntity = client.CreateService<IMatchEntityEndpoints>();
            filepath = Path.Combine(path, "MatchEntity.csv");
            using (var csvWriter = new CsvWritingStream(filepath).CsvWriter)
            {
                int page = 1;
                PagedResponse<MatchEntityBackup> response;

                do
                {
                    response = await matchEntity.GetMatchEntityBackup(new PagedRequest(page, 10000));
                    csvWriter.WriteRecords(response.Items);
                    page++;
                }
                while (response.HasNextPage);
            }
            log.Information("Created file for Match Entity {path}", filepath);



        }
    }

    public sealed class TestCsvReadOperation : CSVReadOperation
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

    public sealed class TestCsvWriteOperation : CSVWriteOperation
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

    public sealed class GenerateMakeModelMatch : CSVWriteOperation
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
