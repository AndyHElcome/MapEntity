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
using System.Threading.Tasks;

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
        public CsvReader CsvReader => new CsvReader(Reader, new CsvConfiguration(CultureInfo.InvariantCulture) { Encoding = Encoding.UTF8 });

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
                    csvWriter.WriteRecords(matchBases.Documents.Select(c => c.BuildCsvObject()));
                }

                log.Information("Created file for Match {matchBaseType} {path}", matchBaseType.ToString(), filepath);
            }


            var matchEntity = client.CreateService<IMatchEntityEndpoints>();
            filepath = Path.Combine(path, "MatchEntity.csv");
            using (var csvWriter = new CsvWritingStream(filepath).CsvWriter)
            {
                string? cursor = null;
                int page = 1;
                PagedCursorResponse<MatchEntityBackup> response;

                do
                {
                    response = await matchEntity.GetMatchEntityBackup(cursor, 1000);
                    csvWriter.WriteRecords(response.Documents);
                    page++;
                    cursor = response.Cursor;
                }
                while (response.HasNextPage);
            }
            log.Information("Created file for Match Entity {path}", filepath);


            var entityRelation = client.CreateService<IEntityRelationEndpoints>();
            filepath = Path.Combine(path, "CurrentRelations.csv");
            using (var csvWriter = new CsvWritingStream(filepath).CsvWriter)
            {
                var response = await entityRelation.GetCurrentEntityRelations();
                csvWriter.WriteRecords(response.Documents);
            }
            log.Information("Created file for Current Relations {path}", filepath);

            filepath = Path.Combine(path, "PreviousRelations.csv");
            using (var csvWriter = new CsvWritingStream(filepath).CsvWriter)
            {
                var response = await entityRelation.GetPreviousEntityRelations();
                csvWriter.WriteRecords(response.Documents);
            }
            log.Information("Created file for Previous Relations {path}", filepath);
        }
    }

    public sealed class CSVBackupInitialise(string csvPath, string MMIv8Path, string TecDocPath, string EntityRelationPath) : ICSVOperation
    {
        public string CSVPath { get; set; } = csvPath;

        public async Task ExecuteOperation(ILogger log)
        {
            var client = new RefitClient(log, 5);

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
            Task loadEntityRelationTask = new LoadEntityRelation(filepath, legacyversion.Value.VersionNumber).ExecuteOperation(log);

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

            List<SourceMMIv8> entities = [];
            using (var csvReader = CsvStream.CsvReader)
            {
                csvReader.Context.RegisterClassMap<PutSourceMMIv8RequestMap>();

                csvReader.Read();
                csvReader.ReadHeader();
                while (csvReader.Read())
                {
                    var record = csvReader.GetRecord<PutSourceMMIv8Request>();
                    entities.Add(record.ToSourceMMIv8(client.VersionProvider));
                }
            }

            foreach (var batch in entities.Chunk(1000))
            {
                await sourceEntity.Bulkload(batch); //TODO Create return types
            }

            log.Information("Deleted All Entities and reloaded: {CSVPath} ({count} records)", CSVPath, entities.Count);
        }
    }

    public sealed class UpdateMMIv8Entity(string csvPath) : CSVReadOperation(csvPath)
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            var client = new RefitClient(log);
            var sourceEntity = client.CreateService<ISourceMMIv8Endpoints>();

            int count = 0;
            using (var csvReader = CsvStream.CsvReader)
            {
                csvReader.Context.RegisterClassMap<PutSourceMMIv8RequestMap>();

                csvReader.Read();
                csvReader.ReadHeader();
                while (csvReader.Read())
                {
                    var record = csvReader.GetRecord<PutSourceMMIv8Request>();
                    await sourceEntity.UpdateEntity(record.ToSourceMMIv8(client.VersionProvider));
                    count++;
                }
            }

            log.Information("Updated {count} Entities: {CSVPath}", count, CSVPath);
        }
    }

    public sealed class ReloadTecDocPCEntities(string csvPath) : CSVReadOperation(csvPath)
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            var client = new RefitClient(log);
            var sourceEntity = client.CreateService<ISourceTecDocPCEndpoints>();

            await sourceEntity.DeleteAll();

            List<SourceTecDocPC> entities = [];
            using (var csvReader = CsvStream.CsvReader)
            {
                csvReader.Context.RegisterClassMap<PutSourceTecDocPCRequestMap>();

                csvReader.Read();
                csvReader.ReadHeader();
                while (csvReader.Read())
                {
                    var record = csvReader.GetRecord<PutSourceTecDocPCRequest>();
                    entities.Add(record.ToSourceTecDocPC(client.VersionProvider));
                }
            }

            foreach (var batch in entities.Chunk(1000))
            {
                await sourceEntity.Bulkload(batch); //TODO Create return types
            }

            log.Information("Deleted All Entities and reloaded: {CSVPath} ({count} records)", CSVPath, entities.Count);
        }
    }

    public sealed class UpdateTecDocEntity(string csvPath) : CSVReadOperation(csvPath)
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            var client = new RefitClient(log);
            var sourceEntity = client.CreateService<ISourceTecDocPCEndpoints>();

            int count = 0;
            using (var csvReader = CsvStream.CsvReader)
            {
                csvReader.Context.RegisterClassMap<PutSourceTecDocPCRequestMap>();

                csvReader.Read();
                csvReader.ReadHeader();
                while (csvReader.Read())
                {
                    var record = csvReader.GetRecord<PutSourceTecDocPCRequest>();
                    await sourceEntity.UpdateEntity(record.ToSourceTecDocPC(client.VersionProvider));
                    count++;
                }
            }

            log.Information("Updated {count} Entities: {CSVPath}", count, CSVPath);
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

            foreach (var batch in entityRelations.Chunk(1000))
            {
                await entityRelationEndpoints.CreateEntityRelation(VersionNumber, batch.ToList()); //TODO Create return types
            }

            log.Information("Loaded {count} Previous Matches for Version {vesionNumber}", entityRelations.Count, VersionNumber);
        }
    }

    public sealed class UpdateMatchBaseScore(string csvPath) : CSVReadOperation(csvPath)
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            var matchBaseEndpoints = new RefitClient(log, 5).CreateService<IMatchBaseEndpoints>();
            int count = 0;

            using (var csvReader = CsvStream.CsvReader)
            {
                csvReader.Context.RegisterClassMap<PutMatchBaseRequestMap>();

                csvReader.Read();
                csvReader.ReadHeader();
                while (csvReader.Read())
                {
                    var putMatchBaseRequest = csvReader.GetRecord<PutMatchBaseRequest>();

                    await matchBaseEndpoints.UpdateMatchBaseScore(putMatchBaseRequest.MatchBaseType, putMatchBaseRequest.MatchHash, putMatchBaseRequest.NewScore);
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
            var matchBaseEndpoints = new RefitClient(log, 5).CreateService<IMatchBaseEndpoints>();
            int count = 0;

            using (var csvReader = CsvStream.CsvReader)
            {
                csvReader.Context.RegisterClassMap<PutMatchBaseRequestMap>();

                csvReader.Read();
                csvReader.ReadHeader();
                while (csvReader.Read())
                {
                    var putMatchBaseRequest = csvReader.GetRecord<PutMatchBaseRequest>();

                    await matchBaseEndpoints.StorePartialMatchBase(putMatchBaseRequest.MatchBaseType, putMatchBaseRequest.MatchHash, putMatchBaseRequest.NewScore);
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
            var matchMakeModelEndpoints = new RefitClient(log, 5).CreateService<IMatchMakeModelEndpoints>();
            int count = 0;

            //List<Task> tasks = new();
            using (var csvReader = CsvStream.CsvReader)
            {
                csvReader.Context.RegisterClassMap<MatchMakeModelRequestMap>();

                csvReader.Read();
                csvReader.ReadHeader();
                while (csvReader.Read())
                {
                    var record = csvReader.GetRecord<MatchMakeModelRequest>();
                    await matchMakeModelEndpoints.CreateMakeModelMatch(record);
                    //tasks.Add( matchMakeModelEndpoints.CreateMakeModelMatch(record));
                    count++;
                }
            }

            //await Task.WhenAll( tasks );

            log.Information("Loaded file: {CSVPath} ({count} records)", CSVPath, count);
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
