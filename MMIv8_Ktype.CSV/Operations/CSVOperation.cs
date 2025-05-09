using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MMIv8_Ktype.Api;
using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.CSV.Converters;
using MMIv8_Ktype.CSV.Maps;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Indexes;
using MMIv8_Ktype.Models.Util;
using Serilog;
using SharpCompress.Common;
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
        //public CsvReader CsvReader => new CsvReader(Reader, CultureInfo.InvariantCulture);
        public CsvReader CsvReader => new CsvReader(Reader, new CsvConfiguration(CultureInfo.InvariantCulture) {Encoding = Encoding.UTF8 });

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


            var entityRelation = client.CreateService<IEntityRelationEndpoints>();
            filepath = Path.Combine(path, "CurrentRelations.csv");
            using (var csvWriter = new CsvWritingStream(filepath).CsvWriter)
            {
                var response = await entityRelation.GetCurrentEntityRelations();
                csvWriter.WriteRecords(response);
            }
            log.Information("Created file for Current Relations {path}", filepath);

            filepath = Path.Combine(path, "PreviousRelations.csv");
            using (var csvWriter = new CsvWritingStream(filepath).CsvWriter)
            {
                var response = await entityRelation.GetPreviousEntityRelations();
                csvWriter.WriteRecords(response);
            }
            log.Information("Created file for Previous Relations {path}", filepath);
        }
    }

    public sealed class CSVBackupInitialise(string csvPath, string MMIv8Path, string TecDocPath, string EntityRelationPath) : ICSVOperation
    {
        public string CSVPath { get; set; } = csvPath;

        public async Task ExecuteOperation(ILogger log)
        {
            var client = new RefitClient(log);

            Task deleteMatchEntityTask = client.CreateService<IMatchEntityEndpoints>().DeleteAll();
            Task deleteMatchMakeModelTask = client.CreateService<IMatchMakeModelEndpoints>().DeleteAll();
            Task deleteEntityRelationTask = client.CreateService<IEntityRelationEndpoints>().DeleteAll();
            Task deleteMatchBaseTask = client.CreateService<IMatchBaseEndpoints>().DeleteAll();
            Task deleteVersionTask = client.CreateService<IVersionEndpoints>().DeleteAll();
            //Task deleteVersionTask = client.CreateService<>().DeleteAll(); //TODO Delete all users

            var path = Path.GetFullPath(CSVPath);
            string filepath;

            await deleteVersionTask;
            var legacyversion = await client.CreateService<IVersionEndpoints>().CreateVersion(new CreateVersionRequest("Legacy", "Legacy", "Admin"));
            var currentversion = await client.CreateService<IVersionEndpoints>().CreateVersion(new CreateVersionRequest("0", "0", "Admin"));


            filepath = Path.IsPathRooted(MMIv8Path) ? MMIv8Path : Path.Combine(path, MMIv8Path);
            Task loadMMITask = new ReloadMMIv8Entities(filepath).ExecuteOperation(log);

            filepath = Path.IsPathRooted(TecDocPath) ? TecDocPath : Path.Combine(path, TecDocPath);
            Task loadTDTask = new ReloadTecDocPCEntities(filepath).ExecuteOperation(log);

            await Task.WhenAll(deleteEntityRelationTask, deleteMatchEntityTask);
            filepath = Path.IsPathRooted(EntityRelationPath) ? EntityRelationPath : Path.Combine(path, EntityRelationPath);
            Task loadEntityRelationTask = new LoadEntityRelation(filepath, legacyversion.VersionNumber).ExecuteOperation(log);

            await Task.WhenAll(deleteMatchMakeModelTask, loadMMITask, loadTDTask, loadEntityRelationTask);
            filepath = Path.Combine(path, "MatchMakeModel.csv");
            await new ImportMakeModelMatch(filepath).ExecuteOperation(log);

            await deleteMatchBaseTask;
            filepath = Path.Combine(path, "MatchBody.csv");
            Task MatchBody = new UpdateMatchBaseScore(filepath).ExecuteOperation(log);
            filepath = Path.Combine(path, "MatchDrive.csv");
            Task MatchDrive = new UpdateMatchBaseScore(filepath).ExecuteOperation(log);
            filepath = Path.Combine(path, "MatchFuel.csv");
            Task MatchFuel = new UpdateMatchBaseScore(filepath).ExecuteOperation(log);

            filepath = Path.Combine(path, "MatchMark.csv");
            Task MatchMark = new StorePartialMatchBase(filepath).ExecuteOperation(log);
            filepath = Path.Combine(path, "MatchIdentifier.csv");
            Task MatchIdentifier = new StorePartialMatchBase(filepath).ExecuteOperation(log);


            await Task.WhenAll(MatchBody, MatchDrive, MatchFuel, MatchMark, MatchIdentifier);

            //TODO Add Read for checked EntityMatches
            //TODO Add Read for checked EntityMatches


        }
    }

    public sealed class ReloadMMIv8Entities(string csvPath) : CSVReadOperation(csvPath)
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            var client = new RefitClient(log);
            var sourceEntity = client.CreateService<ISourceMMIv8Endpoints>();

            await sourceEntity.DeleteAll();

            int count = 0;
            using (var csvReader = CsvStream.CsvReader)
            {
                var NAtoIntConverter = new NAtoIntConverter();
                var NAtoDecimalConverter = new NAtoDecimalConverter();

                csvReader.Read();
                csvReader.ReadHeader();
                while (csvReader.Read())
                {
                    var record = new MongoSourceMMIv8(csvReader.GetField<int>("MMI V8 Key"), client.VersionProvider)
                    {
                        Manufacturer = csvReader.GetField("Manufacturer"),
                        Model = csvReader.GetField("Model"),
                        SubModel = csvReader.GetField("SubModel"),
                        Mark_or_Series = csvReader.GetField("Mark or Series"),
                        Identifier = csvReader.GetField("Identifier"),
                        Engine_Size = csvReader.GetConverted<decimal>("Engine Size", NAtoDecimalConverter),
                        Cylinders = csvReader.GetConverted<int>("Cylinders", NAtoIntConverter),
                        Cylinder_Layout = csvReader.GetField("Cylinder Layout"),
                        Cam = csvReader.GetField("Cam"),
                        Valve = csvReader.GetConverted<int>("Valve", NAtoIntConverter),
                        Start_Month = csvReader.GetConverted<int>("Start Month", NAtoIntConverter),
                        Start_Year = csvReader.GetConverted<int>("Start Year", NAtoIntConverter),
                        End_Month = csvReader.GetConverted<int>("End Month", NAtoIntConverter),
                        End_Year = csvReader.GetConverted<int>("End Year", NAtoIntConverter),
                        Body = csvReader.GetField("Body"),
                        Doors = csvReader.GetConverted<int>("Doors", NAtoIntConverter),
                        Transmission = csvReader.GetField("Transmission"),
                        Gears = csvReader.GetConverted<int>("Gears", NAtoIntConverter),
                        Exact_CC = csvReader.GetConverted<int>("Exact CC", NAtoIntConverter),
                        Drive = csvReader.GetField("Drive"),
                        Fuel = csvReader.GetField("Fuel"),
                        BHP = csvReader.GetConverted<int>("BHP", NAtoIntConverter),
                        KW = csvReader.GetConverted<int>("kW", NAtoIntConverter),
                        Engine_Code = csvReader.GetField("Engine Code"),
                    };

                    await sourceEntity.Create(record);
                    count++;
                }
            }

            log.Information("Deleted All Entities and reloaded: {CSVPath} ({count} records)", CSVPath, count);
        }
    }

    public sealed class ReloadTecDocPCEntities(string csvPath) : CSVReadOperation(csvPath)
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            var client = new RefitClient(log);
            var sourceEntity = client.CreateService<ISourceTecDocPCEndpoints>();

            await sourceEntity.DeleteAll();

            int count = 0;
            using (var csvReader = CsvStream.CsvReader)
            {
                var NAtoIntConverter = new NAtoIntConverter();
                var NAtoDecimalConverter = new NAtoDecimalConverter();

                csvReader.Read();
                csvReader.ReadHeader();
                while (csvReader.Read())
                {
                    var record = new MongoSourceTecDocPC(csvReader.GetField<int>("KTypNr"), client.VersionProvider)
                    {
                        Make             = csvReader.GetField("Make") ?? string.Empty,
                        KModNr           = csvReader.GetField<int>("KModNr"),
                        Model            = csvReader.GetField("Model") ?? string.Empty,
                        Type             = csvReader.GetField("Type") ?? string.Empty,
                        DFrom            = csvReader.GetConverted<int>("dFrom", NAtoIntConverter),
                        DTo              = csvReader.GetConverted<int>("dTo", NAtoIntConverter),
                        KW               = csvReader.GetConverted<int>("KW", NAtoIntConverter),
                        PS               = csvReader.GetConverted<int>("PS", NAtoIntConverter),
                        Litre            = csvReader.GetConverted<decimal>("Litre", NAtoDecimalConverter),
                        Valves =           csvReader.GetConverted<int>("Valves", NAtoIntConverter),
                        Cyl =              csvReader.GetConverted<int>("Cyl", NAtoIntConverter),
                        Drive =            csvReader.GetField("4WD") ?? string.Empty,
                        FuelType =         csvReader.GetField("Fuel Type") ?? string.Empty,
                        BodyType =         csvReader.GetField("Body Type") ?? string.Empty,
                        CCTech =           csvReader.GetConverted<int>("ccTech", NAtoIntConverter),
                        SalesDesc =        csvReader.GetField("SalesDesc") ?? string.Empty,
                        ModelGeneration =  csvReader.GetField("ModelGeneration") ?? string.Empty,
                        TypeDesc =         csvReader.GetField("TypeDesc") ?? string.Empty,
                        Exclude =          csvReader.GetField<bool>("Exclude"),
                        Door =             csvReader.GetConverted<int>("Door", NAtoIntConverter),
                        Region =           csvReader.GetField("Region") ?? string.Empty,
                        LinkedEngineCodes =csvReader.GetField("LinkedEngineCodes") ?? string.Empty,
                    };

                    await sourceEntity.Create(record);
                    count++;
                }
            }

            log.Information("Deleted All Entities and reloaded: {CSVPath} ({count} records)", CSVPath, count);
        }
    }

    public sealed class LoadEntityRelation(string csvPath, int versionNumber) : CSVReadOperation(csvPath)
    {
        public int VersionNumber = versionNumber;
        public async override Task ExecuteOperation(ILogger log)
        {
            var entityRelationEndpoints = new RefitClient(log).CreateService<IEntityRelationEndpoints>();

            await entityRelationEndpoints.DeleteAll();

            List<PutEntityRelationRequest> entityRelations = [];
            using (var csvReader = CsvStream.CsvReader)
            {
                entityRelations = csvReader.GetRecords<PutEntityRelationRequest>().ToList();
            }

            foreach (var mmi in entityRelations.GroupBy(c => c.MMI_V8_Key).ToDictionary(g => g.Key, g => g.ToList()))
            {
                await entityRelationEndpoints.CreateEntityRelation(VersionNumber, mmi.Value); //TODO Create return types
            }

            log.Information("Loaded {count} Previous Matches for Version {vesionNumber}", entityRelations.Count, VersionNumber);
        }
    }

    public sealed class UpdateMatchBaseScore(string csvPath) : CSVReadOperation(csvPath)
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            var matchBaseEndpoints = new RefitClient(log).CreateService<IMatchBaseEndpoints>();
            int count = 0;

            using (var csvReader = CsvStream.CsvReader)
            {
                csvReader.Context.RegisterClassMap<PutMatchBaseRequestMap>();

                csvReader.Read();
                csvReader.ReadHeader();
                while (csvReader.Read())
                {
                    var record = csvReader.GetRecord<PutMatchBaseRequest>();

                    await matchBaseEndpoints.UpdateMatchBaseScore(record);
                    count++;
                }
            }

            log.Information("Loaded file: {CSVPath} ({count} records)", CSVPath, count);
        }
    }

    public sealed class StorePartialMatchBase(string csvPath) : CSVReadOperation(csvPath)
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            var matchBaseEndpoints = new RefitClient(log).CreateService<IMatchBaseEndpoints>();
            int count = 0;

            using (var csvReader = CsvStream.CsvReader)
            {
                csvReader.Context.RegisterClassMap<PutMatchBaseRequestMap>();

                csvReader.Read();
                csvReader.ReadHeader();
                while (csvReader.Read())
                {
                    var record = csvReader.GetRecord<PutMatchBaseRequest>();

                    await matchBaseEndpoints.StorePartialMatchBase(record);
                    count++;
                }
            }

            log.Information("Loaded file: {CSVPath} ({count} records)", CSVPath, count);
        }
    }

    public sealed class ImportMakeModelMatch(string csvPath) : CSVReadOperation(csvPath)
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            var matchMakeModelEndpoints = new RefitClient(log).CreateService<IMatchMakeModelEndpoints>();
            int count = 0;

            using (var csvReader = CsvStream.CsvReader)
            {
                csvReader.Context.RegisterClassMap<MatchMakeModelRequestMap>();

                csvReader.Read();
                csvReader.ReadHeader();
                while (csvReader.Read())
                {
                    var record = csvReader.GetRecord<MatchMakeModelRequest>();

                    await matchMakeModelEndpoints.CreateMakeModelMatch(record);
                    count++;
                }
            }

            log.Information("Loaded file: {CSVPath} ({count} records)", CSVPath, count);
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
