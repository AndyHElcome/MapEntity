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
using System.Text.Json;
using System.Threading.Tasks;

namespace MMIv8_Ktype.CSV.Operations
{
    public abstract class CSVReadOperation(string csvPath) : ICSVOperation
    {
        public string CSVPath { get; set; } = csvPath;

        public CSVReadingStream CsvStream => new(CSVPath);

        public abstract Task ExecuteOperation(ILogger log);
    }

    public abstract class CSVWriteOperation(string csvPath, bool append = false) : ICSVOperation
    {
        public string CSVPath { get; set; } = csvPath;
        public bool Append { get; set; } = append;

        public CSVWritingStream CsvStream => new(CSVPath, Append);

        public abstract Task ExecuteOperation(ILogger log);
    }

    public class CSVWritingStream(string csvPath, bool append = false) : IDisposable
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

    public class CSVReadingStream(string csvPath) : IDisposable
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

    public sealed class DEBUG(string csvPath) : ICSVOperation
    {
        public string CSVPath { get; set; } = csvPath;

        public async Task ExecuteOperation(ILogger log)
        {

            var path = Path.GetFullPath(CSVPath);
            string filepath;


            filepath = Path.Combine(path, "MatchMark.csv");
            Task MatchMark = new StorePartialMatchBase(filepath).ExecuteOperation(log);
            await Task.WhenAll(MatchMark);

            //filepath = Path.Combine(path, "MatchBody.csv");
            //Task MatchBody = new UpdateMatchBaseScore(filepath).ExecuteOperation(log);
            //filepath = Path.Combine(path, "MatchDrive.csv");
            //Task MatchDrive = new UpdateMatchBaseScore(filepath).ExecuteOperation(log);
            //filepath = Path.Combine(path, "MatchFuel.csv");
            //Task MatchFuel = new UpdateMatchBaseScore(filepath).ExecuteOperation(log);

            //await Task.WhenAll(MatchBody, MatchDrive, MatchFuel);

            //filepath = Path.Combine(path, "MatchedMatches.csv");
            //Task MatchedMatches = new UpdateMatchedFlag(filepath).ExecuteOperation(log);
            //filepath = Path.Combine(path, "FailedMatches.csv");
            //Task FailedMatches = new UpdateFailedFlag(filepath).ExecuteOperation(log);

            //await Task.WhenAll(MatchedMatches, FailedMatches);

            //filepath = Path.Combine(path, "CheckedMMIs.csv");
            //Task CheckedMMIs = new UpdateMatchRefineStatus(filepath).ExecuteOperation(log);
            //filepath = Path.Combine(path, "CheckMMIs.csv");
            //Task CheckMMIs = new ResetMatchResult(filepath).ExecuteOperation(log);

            //await Task.WhenAll(CheckedMMIs, CheckMMIs);
            //await Task.WhenAll(CheckedMMIs);
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
            using (var csvWriter = new CSVWritingStream(filepath).CsvWriter)
            {
                csvWriter.Context.RegisterClassMap<MatchMakeModelMap>();
                csvWriter.WriteRecords((await matchMakeModel.GenerateMakeModelMatch()).Value!);
            }
            log.Information("Created file for Match Make Model {path}", filepath);


            var matchBase = client.CreateService<IMatchBaseEndpoints>();
            foreach (var matchBaseType in (MatchBaseType[])Enum.GetValues(typeof(MatchBaseType)))
            {
                filepath = Path.Combine(path, $"Match{matchBaseType.ToString()}.csv");

                try
                {
                    var matchBases = await matchBase.GetByMatchBaseType(matchBaseType);
                    using (var csvWriter = new CSVWritingStream(filepath).CsvWriter)
                    {
                        csvWriter.WriteRecords(matchBases.Value!.Documents.Select(c => c.BuildCsvObject()));
                    }
                }
                catch(Exception ex)
                {
                    log.Error(ex ,"Error creating file for Match {matchBaseType} {path}", matchBaseType.ToString(), filepath);
                    continue;
                }

                log.Information("Created file for Match {matchBaseType} {path}", matchBaseType.ToString(), filepath);
            }


            var matchEntity = client.CreateService<IMatchEntityEndpoints>();
            filepath = Path.Combine(path, "...");
            using (var csvWriterMatched = new CSVWritingStream(Path.Combine(path, "MatchedMatches.csv")).CsvWriter)
            using (var csvWriterFailed = new CSVWritingStream(Path.Combine(path, "FailedMatches.csv")).CsvWriter)
            {
                string? cursor = null;
                int page = 1;
                SerializableResult<PagedCursorResponse<MatchEntityBackup>> response;

                do
                {
                    response = await matchEntity.GetMatchEntityBackup(cursor, 1000);
                    if (!response.IsSuccess)
                        throw new Exception(response.Error!.ToString());

                    var results = response.Value!.Documents.Where(c => c.Matched).Select(c => new UpdateFlagRequest(c.KTypNr, c.MMI_V8_Key, c.Matched, c.MatchDetail));
                    csvWriterMatched.WriteRecords(results);

                    results = response.Value!.Documents.Where(c => c.Failed).Select(c => new UpdateFlagRequest(c.KTypNr, c.MMI_V8_Key, c.Failed, c.FailDetail));
                    csvWriterFailed.WriteRecords(results);

                    page++;
                    cursor = response.Value!.Cursor;
                }
                while (response.Value!.HasNextPage);
            }
            log.Information("Created file for Match Entity {path}", filepath);

            filepath = Path.Combine(path, "CheckMMIs.csv");
            using (var csvWriter = new CSVWritingStream(filepath).CsvWriter)
            {
                var response = await matchEntity.GetDistinctMMIv8(IsCheck: true);
                if (!response.IsSuccess)
                    throw new Exception(response.Error!.ToString());
                csvWriter.WriteRecords(response.Value!);
            }
            log.Information("Created file for Previous Relations {path}", filepath);

            filepath = Path.Combine(path, "CheckedMMIs.csv");
            using (var csvWriter = new CSVWritingStream(filepath).CsvWriter)
            {
                var response = await matchEntity.GetDistinctMMIv8(IsCheck: false);
                if (!response.IsSuccess)
                    throw new Exception(response.Error!.ToString());
                csvWriter.WriteRecords(response.Value!);
            }
            log.Information("Created file for Previous Relations {path}", filepath);


            var entityRelation = client.CreateService<IEntityRelationEndpoints>();
            filepath = Path.Combine(path, "CurrentRelations.csv");
            using (var csvWriter = new CSVWritingStream(filepath).CsvWriter)
            {
                var response = await entityRelation.GetCurrentEntityRelations();
                if (!response.IsSuccess)
                    throw new Exception(response.Error!.ToString());
                csvWriter.WriteRecords(response.Value!.Documents);
            }
            log.Information("Created file for Current Relations {path}", filepath);

            filepath = Path.Combine(path, "PreviousRelations.csv");
            using (var csvWriter = new CSVWritingStream(filepath).CsvWriter)
            {
                var response = await entityRelation.GetPreviousEntityRelations();
                if (!response.IsSuccess)
                    throw new Exception(response.Error!.ToString());
                csvWriter.WriteRecords(response.Value!.Documents);
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

            var deleteMatchEntityTask = client.CreateService<IMatchEntityEndpoints>().DeleteAll();
            var deleteMatchMakeModelTask = client.CreateService<IMatchMakeModelEndpoints>().DeleteAll();
            var deleteEntityRelationTask = client.CreateService<IEntityRelationEndpoints>().DeleteAll();
            var deleteMatchBaseTask = client.CreateService<IMatchBaseEndpoints>().DeleteAll();
            var deleteVersionTask = client.CreateService<IVersionEndpoints>().DeleteAll();
            var deleteUserTask = client.CreateService<IUserEndpoints>().DeleteAll();

            var path = Path.GetFullPath(CSVPath);
            string filepath;

            await Task.WhenAll(deleteVersionTask, deleteUserTask);
            var user = await client.CreateService<IUserEndpoints>().Create("Admin");
            var legacyversion = await client.CreateService<IVersionEndpoints>().CreateVersion("Legacy", "Legacy", "Admin");
            var currentversion = await client.CreateService<IVersionEndpoints>().CreateVersion("0", "0", "Admin");


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

            filepath = Path.Combine(path, "MatchedMatches.csv");
            Task MatchedMatches = new UpdateMatchedFlag(filepath).ExecuteOperation(log);
            filepath = Path.Combine(path, "FailedMatches.csv");
            Task FailedMatches = new UpdateFailedFlag(filepath).ExecuteOperation(log);

            await Task.WhenAll(MatchedMatches, FailedMatches);

            filepath = Path.Combine(path, "CheckedMMIs.csv");
            Task CheckedMMIs = new UpdateMatchRefineStatus(filepath).ExecuteOperation(log);
            //filepath = Path.Combine(path, "CheckMMIs.csv");
            //Task CheckMMIs = new ResetMatchResult(filepath).ExecuteOperation(log);

            //await Task.WhenAll(CheckedMMIs, CheckMMIs);
            await Task.WhenAll(CheckedMMIs);
        }
    }

    public sealed class ReloadMMIv8Entities(string csvPath) : CSVReadOperation(csvPath)
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            var client = new RefitClient(log);
            client.InitialiseVersionProvider();
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
            client.InitialiseVersionProvider();
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
            client.InitialiseVersionProvider();
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
            client.InitialiseVersionProvider();
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

            foreach (var batch in entityRelations.Chunk(500))
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
            var matchBaseEndpoints = new RefitClient(log, 15).CreateService<IMatchBaseEndpoints>();
            int count = 0;

            using (var csvReader = CsvStream.CsvReader)
            {
                csvReader.Context.RegisterClassMap<PutMatchBaseRequestWithContextsMap>();

                csvReader.Read();
                csvReader.ReadHeader();
                
                while (csvReader.Read())
                {
                    var putMatchBaseRequest = csvReader.GetRecord<PutMatchBaseRequestWithContexts>();
                    //try
                    //{
                    //    await matchBaseEndpoints.UpdateMatchBaseScore(putMatchBaseRequest.MatchBaseType, putMatchBaseRequest.MatchHash, putMatchBaseRequest.NewScore);
                    //}
                    //catch //(Exception ex)
                    //{
                    //    //log.Error(ex, "Error with {@putMatchBaseRequest}", putMatchBaseRequest);
                    //}

                    await matchBaseEndpoints.UpdateMatchBaseScore(putMatchBaseRequest.MatchBaseType, putMatchBaseRequest.MatchHash, putMatchBaseRequest.NewScore);

                    if (!string.IsNullOrWhiteSpace(putMatchBaseRequest.Contexts))
                    {
                        foreach (MatchContext matchContext in JsonSerializer.Deserialize<List<MatchContext>>(putMatchBaseRequest.Contexts)!)
                        {
                            TecDocEntity? tecdocEntity = matchContext.TecDocEntity.DictionaryToObj<TecDocEntity>();
                            MMIEntity? mmiEntity = matchContext.MMIEntity.DictionaryToObj<MMIEntity>();

                            await matchBaseEndpoints.AddMatchBaseContext(putMatchBaseRequest.MatchHash, new AddMatchContext(tecdocEntity, mmiEntity, matchContext.ScoreOverride));
                        }
                    }
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
            var matchBaseEndpoints = new RefitClient(log, 15).CreateService<IMatchBaseEndpoints>();
            int count = 0;

            using (var csvReader = CsvStream.CsvReader)
            {
                csvReader.Context.RegisterClassMap<PutMatchBaseRequestWithContextsMap>();

                csvReader.Read();
                csvReader.ReadHeader();
                while (csvReader.Read())
                {
                    var putMatchBaseRequest = csvReader.GetRecord<PutMatchBaseRequestWithContexts>();

                    await matchBaseEndpoints.StorePartialMatchBase(putMatchBaseRequest.MatchBaseType, putMatchBaseRequest.MatchHash, putMatchBaseRequest.NewScore);

                    if (!string.IsNullOrWhiteSpace(putMatchBaseRequest.Contexts))
                    { 
                        foreach(MatchContext matchContext in JsonSerializer.Deserialize<List<MatchContext>>(putMatchBaseRequest.Contexts)!)
                        {
                            TecDocEntity? tecdocEntity = matchContext.TecDocEntity.DictionaryToObj<TecDocEntity>(false);
                            MMIEntity? mmiEntity = matchContext.MMIEntity.DictionaryToObj<MMIEntity>(false);

                            await matchBaseEndpoints.AddMatchBaseContext(putMatchBaseRequest.MatchHash, new AddMatchContext(tecdocEntity, mmiEntity, matchContext.ScoreOverride));
                        }
                    }

                    count++;
                }
            }

            log.Information("Loaded file: {CSVPath} ({count} records)", CSVPath, count);
        }
    }

    public sealed class UpdateMatchedFlag(string csvPath) : CSVReadOperation(csvPath)
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            var matchEntityEndpoints = new RefitClient(log, 5).CreateService<IMatchEntityEndpoints>();
            int count = 0;

            using (var csvReader = CsvStream.CsvReader)
            {
                csvReader.Context.AutoMap<UpdateFlagRequest>();

                csvReader.Read();
                csvReader.ReadHeader();
                while (csvReader.Read())
                {
                    var request = csvReader.GetRecord<UpdateFlagRequest>();

                    await matchEntityEndpoints.UpdateMatchedFlag(request);
                    count++;
                }
            }

            log.Information("Loaded file: {CSVPath} ({count} records)", CSVPath, count);
        }
    }

    public sealed class UpdateFailedFlag(string csvPath) : CSVReadOperation(csvPath)
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            var matchEntityEndpoints = new RefitClient(log, 5).CreateService<IMatchEntityEndpoints>();
            int count = 0;

            using (var csvReader = CsvStream.CsvReader)
            {
                csvReader.Context.AutoMap<UpdateFlagRequest>();

                csvReader.Read();
                csvReader.ReadHeader();
                while (csvReader.Read())
                {
                    var request = csvReader.GetRecord<UpdateFlagRequest>();

                    await matchEntityEndpoints.UpdateFailedFlag(request);
                    count++;
                }
            }

            log.Information("Loaded file: {CSVPath} ({count} records)", CSVPath, count);
        }
    }

    public sealed class UpdateMatchRefineStatus(string csvPath) : CSVReadOperation(csvPath)
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            var matchEntityEndpoints = new RefitClient(log, 5).CreateService<IMatchEntityEndpoints>();
            int count = 0;

            using (var csvReader = CsvStream.CsvReader)
            {
                csvReader.Context.AutoMap<MMI_V8_Key>();

                csvReader.Read();
                csvReader.ReadHeader();
                while (csvReader.Read())
                {
                    var request = csvReader.GetRecord<MMI_V8_Key>();

                    await matchEntityEndpoints.UpdateMatchRefineStatus(request.ExternalId);
                    count++;
                }
            }

            log.Information("Loaded file: {CSVPath} ({count} records)", CSVPath, count);
        }
    }

    public sealed class ResetMatchResult(string csvPath) : CSVReadOperation(csvPath)
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            var matchEntityEndpoints = new RefitClient(log, 5).CreateService<IMatchEntityEndpoints>();
            int count = 0;

            using (var csvReader = CsvStream.CsvReader)
            {
                csvReader.Context.AutoMap<MMI_V8_Key>();

                csvReader.Read();
                csvReader.ReadHeader();
                while (csvReader.Read())
                {
                    var request = csvReader.GetRecord<MMI_V8_Key>();

                    await matchEntityEndpoints.ResetMatchResult(request.ExternalId);
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

                    //if (record.MMI_SourceEntityModelHash !="" && record.TD_SourceEntityModelHash !="")
                    await matchMakeModelEndpoints.CreateMatchMakeModel(record.TD_SourceEntityModelHash, record.MMI_SourceEntityModelHash);
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
                csvWriter.WriteRecords(matchMakeModels.Value!);
            }

            log.Information("Created file: {CSVPath} ({count} records)", CSVPath, matchMakeModels.Value!.Count);
        }
    }
}
