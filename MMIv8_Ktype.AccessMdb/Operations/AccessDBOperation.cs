using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MMIv8_Ktype.AccessMdb.Maps;
using MMIv8_Ktype.Api;
using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Outputs;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using Serilog;
using System.Data;
using System.Data.OleDb;
using System.Dynamic;
using System.Formats.Asn1;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text.Json;
using static MMIv8_Ktype.AccessMdb.Operations.GenerateMMIEntities;

namespace MMIv8_Ktype.AccessMdb.Operations
{
    public abstract class AccessDBOperation(string dBPath, string tableName) : IAccessDBOperation
    {
        public string DBPath { get; set; } = dBPath;
        public string TableName { get; set; } = tableName;

        internal string QueryString = $"SELECT * FROM [{tableName}]";
        internal DataSet DBDataSet = new();

        internal JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions().GetJsonSerializerOptions();

        public abstract Task ExecuteOperation(ILogger log);

        public delegate Task<PagedResponse<T>> GetPagedDocumentsDelegate<T>(int page, int pageSize);
        public delegate Task<PagedCursorResponse<T>> GetPagedDocumentsDelegatev2<T>(string? cursor, int pageSize);
        public delegate Task<List<T>> GetDocumentsDelegate<T>();
        public delegate Task<T> GetDocumentsByIdDelegate<T, Tid>(Tid documentId);
        public delegate TOut ConvertDocumentToDataRowObject<T, TOut>(T document);
        public delegate TCursor ConvertDocumentIdToCursor<Tid, TCursor>(Tid documentId);

        public async Task GenerateTableFromPagedCursor<T, TObjType, TDocumentId>(
            string tableName,
            ILogger log,
            GetPagedDocumentsDelegatev2<T> getPagedDocumentsFunc,
            ConvertDocumentToDataRowObject<T, TObjType> convertToRow,
            string[] primaryKeys,
            int pageSize = 1000,
            bool append = false)
        {
            if (!append)
                DropTable(tableName, log);

            string? cursor = null;
            var headerDocument = await getPagedDocumentsFunc(cursor, 1);

            if (headerDocument is null or { Documents.Count: 0 } or { TotalDocuments: 0 })
                return;
            if (headerDocument is null || headerDocument.Documents.Count == 0 || headerDocument.TotalDocuments == 0)
                throw new Exception("Pattern Matching didn't work");

            DataTable dataTable = AddNewTableToDataSet(convertToRow(headerDocument!.Documents!.FirstOrDefault()!)!, tableName, primaryKeys, log) ?? throw new NoNullAllowedException();

            PagedCursorResponse<T> response;
            do
            {
                response = await getPagedDocumentsFunc(cursor, pageSize);
                foreach (T document in response.Documents)
                {
                    try
                    {
                        var obj = convertToRow(document);
                        var newRow = dataTable.NewRow().ConvertObjToDataRow(obj);
                        dataTable.Rows.Add(newRow);
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex, "Error writing {@item}", document);
                    }
                }

                cursor = response.Cursor;
            }
            while (response.HasNextPage);

            CommitChanges(tableName, log);

            log.Information("Loaded {tableCount} / {apicount} records into {table}", dataTable.Rows.Count, response.TotalDocuments, tableName);
        }

        public async Task GenerateTable<T, TObjType>(
            string tableName,
            ILogger log,
            GetPagedDocumentsDelegate<T> getPagedDocumentsFunc,
            ConvertDocumentToDataRowObject<T, TObjType> convertToRow,
            string[] primaryKeys,
            bool append = false)
        {
            if (!append)
                DropTable(tableName, log);

            var headerDocument = await getPagedDocumentsFunc(1, 1);

            if (headerDocument is null or { Documents.Count: 0 } or { TotalDocuments: 0 })
                return;
            if (headerDocument is null || headerDocument.Documents.Count == 0 || headerDocument.TotalDocuments == 0)
                throw new Exception("Pattern Matching didn't work");


            DataTable dataTable = AddNewTableToDataSet(convertToRow(headerDocument!.Documents!.FirstOrDefault()!)!, tableName, primaryKeys, log) ?? throw new NoNullAllowedException();

            int page = 1;
            PagedResponse<T> response;
            do
            {
                response = await getPagedDocumentsFunc(page, 1000);
                foreach (T document in response.Documents)
                {
                    try
                    {
                        var obj = convertToRow(document);
                        var newRow = dataTable.NewRow().ConvertObjToDataRow(obj);
                        dataTable.Rows.Add(newRow);
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex, "Error writing {@item}", document);
                    }
                }
                page++;
            }
            while (response.HasNextPage);

            CommitChanges(tableName, log);

            log.Information("Loaded {tableCount} / {apicount} records into {table}", dataTable.Rows.Count, response.TotalDocuments, tableName);
        }

        public async Task GenerateTable<T, TObjType>(
            string tableName,
            ILogger log,
            GetDocumentsDelegate<T> getDocumentsFunc,
            ConvertDocumentToDataRowObject<T, TObjType> convertToRow,
            string[] primaryKeys,
            bool append = false)
        {
            if (!append)
                DropTable(tableName, log);

            var response = await getDocumentsFunc();

            if (response is null or { Count: 0 })
                return;
            if (response is null || response.Count == 0)
                throw new Exception("Pattern Matching didn't work");


            DataTable dataTable = AddNewTableToDataSet(convertToRow(response!.FirstOrDefault()!)!, tableName, primaryKeys, log) ?? throw new NoNullAllowedException();

            foreach (T document in response)
            {
                try
                {
                    var obj = convertToRow(document);
                    var newRow = dataTable.NewRow().ConvertObjToDataRow(obj);
                    dataTable.Rows.Add(newRow);
                }
                catch (Exception ex)
                {
                    log.Error(ex, "Error writing {@item}", document);
                }
            }

            CommitChanges(tableName, log);

            log.Information("Loaded {tableCount} / {apicount} records into {table}", dataTable.Rows.Count, response.Count, tableName);
        }

        public OleDbConnection DBConnection()
        {
            var builder = new OleDbConnectionStringBuilder
            {
                Provider = "Microsoft.ACE.OLEDB.12.0",
                DataSource = DBPath
            };

            return new OleDbConnection(builder.ToString());
        }

        public bool TableExists(string tableName)
        {
            using var conn = DBConnection();
            conn.Open();

            DataTable? schema = conn.GetOleDbSchemaTable(
                OleDbSchemaGuid.Tables,
                new object[] { null, null, tableName, "TABLE" });

            return schema != null && schema.Rows.Count > 0;
        }

        public DataTable AddTableToDataSet(string tableName, ILogger log)
        {
            if (TableExists(tableName))
            {
                using (OleDbConnection conn = DBConnection())
                using (OleDbCommand cmd = new(QueryString, conn))
                {
                    conn.Open();

                    OleDbDataAdapter adapter = new(cmd);

                    adapter.FillLoadOption = LoadOption.OverwriteChanges;
                    adapter.Fill(DBDataSet, tableName);

                    conn.Close();

                    Log.Information("Loaded table {tableName} from {query}", tableName, QueryString);
                }

                return DBDataSet.Tables[ tableName ];
            }
            else
            {
                return null;
            }
        }

        public DataTable AddNewTableToDataSet(object obj, string tableName, string[] primaryKeys, ILogger log)
        {
            if (!TableExists(tableName))
            {
                DataTable newTable = Extensions.ConvertObjToNewDataTable(obj, tableName, primaryKeys);

                ExecuteSQLQuery(Extensions.BuildCreateTableSql(newTable), log);

                log.Information("Created table {table}", tableName);

                return AddTableToDataSet(tableName, log);
            }
            else if (!DBDataSet.Tables.Contains(tableName))
            {
                log.Warning("Table {tableName} already exists in Database", tableName);

                return AddTableToDataSet(tableName, log);
            }
            else
            {
                log.Warning("Table {tableName} already exists in DataSet", tableName);

                return DBDataSet.Tables[ tableName ];
            }
        }

        public DataTable AddNewTableToDataSet(Dictionary<string, object> obj, string tableName, string[] primaryKeys, ILogger log)
        {
            if (!TableExists(tableName))
            {
                DataTable newTable = Extensions.ConvertObjToNewDataTable(obj, tableName, primaryKeys);

                ExecuteSQLQuery(Extensions.BuildCreateTableSql(newTable), log);

                log.Information("Created table {table}", tableName);

                return AddTableToDataSet(tableName, log);
            }
            else if (!DBDataSet.Tables.Contains(tableName))
            {
                log.Warning("Table {tableName} already exists in Database", tableName);

                return AddTableToDataSet(tableName, log);
            }
            else
            {
                log.Warning("Table {tableName} already exists in DataSet", tableName);

                return DBDataSet.Tables[ tableName ];
            }
        }

        public void DropTable(string tableName, ILogger log)
        {
            if (TableExists(tableName))
            {
                ExecuteSQLQuery(Extensions.BuildDropTableSql(tableName), log);

                log.Information("Dropped table {table}", tableName);
            }
            else
            {
                log.Warning("Table {tableName} doesn't exist in Database", tableName);
            }

            if (DBDataSet.Tables.Contains(tableName))
            {
                DBDataSet.Tables[ tableName ].Clear();
            }
        }

        public void ExecuteSQLQuery(string queryString, ILogger log)
        {
            using (OleDbConnection conn = DBConnection())
            using (OleDbCommand cmd = new(queryString, conn))
            {
                conn.Open();

                cmd.ExecuteNonQuery();

                conn.Close();
            }

            log.Debug("Ran query: {sql}", queryString);
        }

        public void CommitChanges(string tableName, ILogger log)
        {
            using (OleDbConnection conn = DBConnection())
            {
                conn.Open();

                OleDbDataAdapter adapter = new OleDbDataAdapter(QueryString, conn);

                OleDbCommandBuilder builder = new OleDbCommandBuilder(adapter);

                adapter.UpdateCommand = builder.GetUpdateCommand();
                adapter.InsertCommand = builder.GetInsertCommand();
                adapter.DeleteCommand = builder.GetDeleteCommand();

                // Assuming DBDataSet contains the table
                if (DBDataSet.Tables.Contains(tableName))
                {
                    adapter.Update(DBDataSet, tableName);
                    Log.Debug("Changes committed to table {tableName}", tableName);
                }
                else
                {
                    Log.Warning("Table {tableName} not found in DataSet", tableName);
                }

                DBDataSet.AcceptChanges();

                conn.Close();
            }
        }
    }

    public class StorePartialMatchBase(string dbPath, string tableName, string outputColumn) : AccessDBOperation(dbPath, tableName)
    {
        public string OutputColumn = outputColumn;

        public async override Task ExecuteOperation(ILogger log)
        {
            IMatchBaseEndpoints matchBaseEndpoints = new RefitClient(log, 5).CreateService<IMatchBaseEndpoints>();

            DataTable dataTable = AddTableToDataSet(TableName, log) ?? throw new NoNullAllowedException();

            foreach (DataRow row in dataTable.Rows)
            {
                try
                {
                    string[] args = row.ItemArray.Select(c => c?.ToString() ?? string.Empty)
                                             .Where(c => c != row[ OutputColumn ].ToString())
                                             .ToArray();

                    PutMatchBaseRequest putMatchBaseRequest = GlobalHelpers.StringToObject<PutMatchBaseRequest>(args);

                    await matchBaseEndpoints.StorePartialMatchBase(putMatchBaseRequest.MatchBaseType, putMatchBaseRequest.MatchHash, putMatchBaseRequest.NewScore); //TODO Create return types

                    row[ OutputColumn ] = "Updated";
                }
                catch (Exception ex)
                {
                    row[ OutputColumn ] = ex.Message;
                    log.Error(ex, "Error in {@args}", row.ItemArray);
                }
            }

            CommitChanges(TableName, log);
        }
    }

    public class UpdateMatchedFlag(string dbPath, string tableName, string outputColumn) : AccessDBOperation(dbPath, tableName)
    {
        public string OutputColumn = outputColumn;

        public async override Task ExecuteOperation(ILogger log)
        {
            IMatchEntityEndpoints matchEntityEndpoints = new RefitClient(log).CreateService<IMatchEntityEndpoints>();

            DataTable dataTable = AddTableToDataSet(TableName, log) ?? throw new NoNullAllowedException();

            foreach (DataRow row in dataTable.Rows)
            {
                try
                {
                    string[] args = row.ItemArray.Select(c => c?.ToString() ?? string.Empty)
                                                 .Where(c => c != row[ OutputColumn ].ToString())
                                                 .ToArray();

                    UpdateFlagRequest record = GlobalHelpers.StringToObject<UpdateFlagRequest>(args);

                    await matchEntityEndpoints.UpdateMatchedFlag(record); //TODO Create return types

                    row[ OutputColumn ] = "Updated";
                }
                catch (Exception ex)
                {
                    row[ OutputColumn ] = ex.Message;
                    log.Error(ex, "Error in {@args}", row.ItemArray);
                }
            }

            CommitChanges(TableName, log);
        }
    }

    public class LoadEntityRelation(string dbPath, string tableName, int versionNumber) : AccessDBOperation(dbPath, tableName)
    {
        public int VersionNumber = versionNumber;

        public async override Task ExecuteOperation(ILogger log)
        {
            IEntityRelationEndpoints entityRelationEndpoints = new RefitClient(log).CreateService<IEntityRelationEndpoints>();

            DataTable dataTable = AddTableToDataSet(TableName, log) ?? throw new NoNullAllowedException();

            var data = dataTable.Select().Select(c => GlobalHelpers.StringToObject<PutEntityRelationRequest>(c.ItemArray.Select(c => c?.ToString() ?? string.Empty).ToArray())).ToList();

            foreach (var batch in data.Chunk(1000))
            {
                await entityRelationEndpoints.CreateEntityRelation(VersionNumber, batch.ToList()); //TODO Create return types
            }

            log.Information("Loaded {count} Previous Matches for Version {vesionNumber}", data?.Count, VersionNumber);
        }
    }

    public class GenerateMakeModelMatch(string dbPath, string tableName) : AccessDBOperation(dbPath, tableName)
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            var matchMakeModelEndpoints = new RefitClient(log).CreateService<IMatchMakeModelEndpoints>();

            await base.GenerateTable(
                TableName,
                log,
                () => matchMakeModelEndpoints.GenerateMakeModelMatch(),
                (document) => (MatchMakeModelRecord)document,
                [ nameof(MatchMakeModelRecord.MatchID) ]
                );
        }
    }

    public class GenerateMatchRefine : AccessDBOperation
    {
        public string? MakeModelMatchId { get; }
        public string? TecDocEntityId { get; }
        public string? MMIv8EntityId { get; }
        public bool? IsCheck { get; }
        public bool? IsMatched { get; }
        public bool? IsFailed { get; }
        public bool? HasDifference { get; }
        public Status[]? Status { get; }
        public bool Append { get; }

        public GenerateMatchRefine(
            string dbPath,
            string tableName,
            string? makeModelMatchId = null,
            string? tecDocEntityId = null,
            string? mmiv8EntityId = null,
            bool? isCheck = null,
            bool? isMatched = null,
            bool? isFailed = null,
            bool? hasDifference = null,
            Status[]? status = null,
            bool append = false) : base(dbPath, tableName)
        {
            MakeModelMatchId = makeModelMatchId;
            TecDocEntityId = tecDocEntityId;
            MMIv8EntityId = mmiv8EntityId;
            IsCheck = isCheck;
            IsMatched = isMatched;
            IsFailed = isFailed;
            HasDifference = hasDifference;
            Status = status;
            Append = append;
        }

        public GenerateMatchRefine(string dbPath, string tableName) : base(dbPath, tableName) { }



        public async override Task ExecuteOperation(ILogger log)
        {
            var matchEntityEndpoints = new RefitClient(log).CreateService<IMatchEntityEndpoints>();

            await base.GenerateTable(
                TableName,
                log,
                () => matchEntityEndpoints.GetAllMatchRefine(MakeModelMatchId, TecDocEntityId, MMIv8EntityId, IsCheck, IsMatched, IsFailed, HasDifference, Status),
                (document) => document,
                [ nameof(MatchRefine.DocumentId) ],
                append: Append
                );
        }
    }

    public class GenerateMatchSummary : AccessDBOperation
    {
        public string? MakeModelMatchId { get; }
        public string? TecDocEntityId { get; }
        public string? MMIv8EntityId { get; }
        public bool? IsCheck { get; }
        public bool? IsMatched { get; }
        public bool? IsFailed { get; }
        public bool? HasDifference { get; }
        public Status[]? Status { get; }
        public bool Append { get; }

        public GenerateMatchSummary(string dbPath, string tableName) : base(dbPath, tableName) { }

        public GenerateMatchSummary(
            string dbPath,
            string tableName,
            string? makeModelMatchId = null,
            string? tecDocEntityId = null,
            string? mmiv8EntityId = null,
            bool? isCheck = null,
            bool? isMatched = null,
            bool? isFailed = null,
            bool? hasDifference = null,
            Status[]? status = null,
            bool append = false) : base(dbPath, tableName)
        {
            MakeModelMatchId = makeModelMatchId;
            TecDocEntityId = tecDocEntityId;
            MMIv8EntityId = mmiv8EntityId;
            IsCheck = isCheck;
            IsMatched = isMatched;
            IsFailed = isFailed;
            HasDifference = hasDifference;
            Status = status;
            Append = append;
        }


        public async override Task ExecuteOperation(ILogger log)
        {
            var matchEntityEndpoints = new RefitClient(log).CreateService<IMatchEntityEndpoints>();

            await base.GenerateTableFromPagedCursor<MatchEntitySummary, MatchEntitySummary, ObjectId>(
                TableName,
                log,
                (string? cursor, int pageSize) => matchEntityEndpoints.GetAllMatchEntitySummary(cursor, pageSize, MakeModelMatchId, TecDocEntityId, MMIv8EntityId, IsCheck, IsMatched, IsFailed, HasDifference, Status),
                (document) => document,
                [ nameof(MatchEntitySummary.DocumentId) ],
                append: Append
                );
        }
    }

    public class GenerateMatchEntity : AccessDBOperation //TODO Tidy up expando building maybe even push to projection
    {
        public string? MakeModelMatchId { get; }
        public string? TecDocEntityId { get; }
        public string? MMIv8EntityId { get; }
        public bool? IsCheck { get; }
        public bool? IsMatched { get; }
        public bool? IsFailed { get; }
        public bool? HasDifference { get; }
        public Status[]? Status { get; }
        public bool Append { get; }

        public GenerateMatchEntity(string dbPath, string tableName) : base(dbPath, tableName) { }

        public GenerateMatchEntity(
            string dbPath,
            string tableName,
            string? makeModelMatchId = null,
            string? tecDocEntityId = null,
            string? mmiv8EntityId = null,
            bool? isCheck = null,
            bool? isMatched = null,
            bool? isFailed = null,
            bool? hasDifference = null,
            Status[]? status = null,
            bool append = false) : base(dbPath, tableName)
        {
            MakeModelMatchId = makeModelMatchId;
            TecDocEntityId = tecDocEntityId;
            MMIv8EntityId = mmiv8EntityId;
            IsCheck = isCheck;
            IsMatched = isMatched;
            IsFailed = isFailed;
            HasDifference = hasDifference;
            Status = status;
            Append = append;
        }


        public async override Task ExecuteOperation(ILogger log)
        {
            var matchEntityEndpoints = new RefitClient(log).CreateService<IMatchEntityEndpoints>();

            await base.GenerateTableFromPagedCursor<MatchEntity, ExpandoObject, ObjectId>(
                TableName,
                log,
                (string? cursor, int pageSize) => matchEntityEndpoints.GetAll(cursor, pageSize, MakeModelMatchId, TecDocEntityId, MMIv8EntityId, IsCheck, IsMatched, IsFailed, HasDifference, Status),
                (MatchEntity document) => new ExpandoObject().BuildExpando(document),
                [ nameof(MatchEntity.DocumentId) ], 
                append: Append
                );
        }
    }

    public class GenerateMatchEntityById : AccessDBOperation //TODO Tidy up expando building maybe even push to projection
    {
        private readonly string tableName;
        public string DocumentId;
        public bool Append;
        public IMatchEntityEndpoints? MatchEntityEndpoints;

        public GenerateMatchEntityById(string dbPath, string tableName, string documentId, bool append) : base(dbPath, tableName)
        {
            this.tableName = tableName;
            DocumentId = documentId;
            Append = append;
            MatchEntityEndpoints = null;
        }
        public GenerateMatchEntityById(string dbPath, string tableName, string documentId, bool append, IMatchEntityEndpoints matchEntityEndpoints) : base(dbPath, tableName)
        {
            this.tableName = tableName;
            DocumentId = documentId;
            Append = append;
            MatchEntityEndpoints = matchEntityEndpoints;
        }

        public async override Task ExecuteOperation(ILogger log)
        {
            if (!Append)
                DropTable(TableName, log);

            MatchEntityEndpoints ??= new RefitClient(log).CreateService<IMatchEntityEndpoints>();

            try
            {
                var objectId = ObjectId.Parse(DocumentId);
                var matchEntity = await MatchEntityEndpoints.GetById(objectId);
                var matchEntityExpando = new ExpandoObject().BuildExpando(matchEntity);

                DataTable dataTable = AddNewTableToDataSet(matchEntityExpando, tableName, [ nameof(MatchEntity.DocumentId) ], log) ?? throw new NoNullAllowedException();

                var newRow = dataTable.NewRow().ConvertObjToDataRow(matchEntityExpando);
                dataTable.Rows.Add(newRow);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Error writing {@item}", DocumentId);
            }
                
            CommitChanges(TableName, log);
        }
    }

    public class GenerateEntityMatchByIds(string dbPath, string tableName, string outputTableName, string inputColumn, string outputColumn, bool append) : AccessDBOperation(dbPath, tableName)
    {
        public string OutputTableName = outputTableName;
        public string OutputColumn = outputColumn;
        public string InputColumn = inputColumn;
        public bool Append = append;

        public async override Task ExecuteOperation(ILogger log)
        {
            if (!Append)
                DropTable(OutputTableName, log);

            DataTable dataTable = AddTableToDataSet(TableName, log) ?? throw new NoNullAllowedException();

            var matchEntityEndpoints = new RefitClient(log).CreateService<IMatchEntityEndpoints>();

            foreach (DataRow row in dataTable.Rows)
            {
                try
                {
                    await new GenerateMatchEntityById(DBPath, OutputTableName, row[ InputColumn ].ToString(), true, matchEntityEndpoints).ExecuteOperation(log); //TODO Create return types

                    row[ OutputColumn ] = "Updated";
                }
                catch (Exception ex)
                {
                    row[ OutputColumn ] = ex.Message;
                    log.Error(ex, "Error in {@args}", row.ItemArray);
                }
            }

            CommitChanges(TableName, log);
        }
    }

    public class GenerateMMIEntities(string dbPath, string tableName) : AccessDBOperation(dbPath, tableName)
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            var sourceEntityEndpoints = new RefitClient(log).CreateService<ISourceMMIv8Endpoints>();

            await base.GenerateTableFromPagedCursor<SourceMMIv8, SourceMMIv8, ObjectId>(
                TableName,
                log,
                (string? cursor, int pageSize) => sourceEntityEndpoints.GetAll(cursor, pageSize),
                (document) => document,
                [ nameof(SourceMMIv8.ExternalId) ],
                10000
                );
        }
    }

    public class GenerateTecDocEntities(string dbPath, string tableName) : AccessDBOperation(dbPath, tableName)
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            var sourceEntityEndpoints = new RefitClient(log).CreateService<ISourceTecDocPCEndpoints>();

            await base.GenerateTableFromPagedCursor<SourceTecDocPC, SourceTecDocPC, ObjectId>(
                TableName,
                log,
                (string? cursor, int pageSize) => sourceEntityEndpoints.GetAll(cursor, pageSize),
                (document) => document,
                [ nameof(SourceTecDocPC.ExternalId) ],
                10000
                );
        }
    }
}
