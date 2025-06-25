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
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Dynamic;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Xml.Linq;
using static MMIv8_Ktype.AccessMdb.Operations.AccessDBOperation;
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

        public delegate Task<SerializableResult<T>> GetDocumentDelegate<T>();
        public delegate Task<SerializableResult<List<T>>> GetDocumentsDelegate<T>();
        public delegate Task<SerializableResult<PagedCursorResponse<T>>> GetPagedDocumentsDelegatev2<T>(string? cursor, int pageSize);
        public delegate IEnumerable<TOut> ConvertDocumentToOutput<T, TOut>(T document);
        public delegate Dictionary<string, object> ConvertDocumentToDictionary<T>(T document);

        public async Task GenerateTableFromDocument<T, TOut>(
            string tableName,
            ILogger log,
            GetDocumentDelegate<T> getDocumentsFunc,
            ConvertDocumentToOutput<T, TOut> documentToOutput,
            ConvertDocumentToDictionary<TOut> outputToDictionary,
            string[] primaryKeys,
            int pageSize = 5000,
            bool append = false)
        {
            if (!append)
                DropTable(tableName, log);

            var response = await getDocumentsFunc();
            if (!response.IsSuccess)
                throw new Exception(response.Error!.ToString());

            if (response.Value is null)
                return;

            List<TOut> output = [ .. documentToOutput(response.Value) ];

            string[] columns = [];
            if (typeof(TOut) == typeof(ExpandoObject))
            {
                var headerDocument = output.First();
                using var dataTable = AddNewTableToDataSet(outputToDictionary(headerDocument), tableName, primaryKeys, log) ?? throw new NoNullAllowedException();
                columns = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();
            }
            else
            {
                using var dataTable = AddNewTableToDataSet(typeof(TOut), tableName, primaryKeys, log) ?? throw new NoNullAllowedException();
                columns = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();
            }

            using var connection = DBConnection();
            connection.Open();
            this.InsertIntoTable(TableName, connection, log, output, outputToDictionary, columns);
            connection.Close();

            log.Information("Loaded {tableCount} records into {table}", output.Count, tableName);
        }

        public async Task GenerateTableFromDocuments<T, TOut>(
            string tableName,
            ILogger log,
            GetDocumentsDelegate<T> getDocumentsFunc,
            ConvertDocumentToOutput<T, TOut> documentToOutput,
            ConvertDocumentToDictionary<TOut> outputToDictionary,
            string[] primaryKeys,
            int pageSize = 2000,
            bool append = false)
        {
            if (!append)
                DropTable(tableName, log);

            var response = await getDocumentsFunc();
            if (!response.IsSuccess)
                throw new Exception(response.Error!.ToString());

            if (response.Value is null or { Count: 0 })
                return;

            string[] columns = [];
            if (typeof(TOut) == typeof(ExpandoObject))
            {
                var headerDocument = documentToOutput(response.Value!.First()).First();
                using var dataTable = AddNewTableToDataSet(outputToDictionary(headerDocument), tableName, primaryKeys, log) ?? throw new NoNullAllowedException();
                columns = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();
            }
            else
            {
                using var dataTable = AddNewTableToDataSet(typeof(TOut), tableName, primaryKeys, log) ?? throw new NoNullAllowedException();
                columns = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();
            }

            using var connection = DBConnection();
            connection.Open();
            int i = 0;

            foreach (var batch in response.Value!.Chunk(pageSize))
            {
                List<TOut> output = new();
                foreach (var document in batch)
                {
                    output.AddRange(documentToOutput(document));
                }

                this.InsertIntoTable(TableName, connection, log, output, outputToDictionary, columns);
                i += output.Count;

                log.Information("Loaded {tableCount} records into {table}", output.Count, tableName);
            }
            connection.Close();

            log.Information("Loaded {tableCount} records into {table}", i, tableName);
        }

        public async Task GenerateTableFromPagedDocuments<T, TOut, TDocumentId>(
            string tableName,
            ILogger log,
            GetPagedDocumentsDelegatev2<T> getDocumentsFunc,
            ConvertDocumentToOutput<T, TOut> documentToOutput,
            ConvertDocumentToDictionary<TOut> outputToDictionary,
            string[] primaryKeys,
            int pageSize = 1000,
            bool append = false)
        {
            if (!append)
                DropTable(tableName, log);

            SerializableResult<PagedCursorResponse<T>> response;
            string[] columns = [];
            if (typeof(TOut) == typeof(ExpandoObject))
            {
                response = await getDocumentsFunc(null, 1);
                if (!response.IsSuccess)
                    throw new Exception(response.Error!.ToString());

                var headerDocument = documentToOutput(response.Value!.Documents.First()).First();
                using var dataTable = AddNewTableToDataSet(outputToDictionary(headerDocument), tableName, primaryKeys, log) ?? throw new NoNullAllowedException();
                columns = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();
            }
            else
            {
                using var dataTable = AddNewTableToDataSet(typeof(TOut), tableName, primaryKeys, log) ?? throw new NoNullAllowedException();
                columns = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();
            }

            using var connection = DBConnection();
            connection.Open();

            int i = 0;
            string? cursor = null;
            do
            {
                response = await getDocumentsFunc(cursor, pageSize);
                
                
                if (!response.IsSuccess)
                    throw new Exception(response.Error!.ToString());

                List<TOut> output = new();

                foreach (T document in response.Value!.Documents)
                {
                    output.AddRange(documentToOutput(document));
                }

                this.InsertIntoTable(TableName, connection, log, output, outputToDictionary, columns);

                i += output.Count;

                log.Information("Loaded {tableCount} records into {table}", output.Count, tableName);

                cursor = response.Value!.Cursor;

                //if (response.Value!.RemmainingDocuments < pageSize)
                    //pageSize = response.Value!.RemmainingDocuments;
            }
            while (response.Value!.HasNextPage);

            connection.Close();
            log.Information("Loaded {tableCount} records into {table}", i, tableName);
        }

        public void InsertIntoTable<T>(
            string tableName,
            OleDbConnection connection,
            ILogger log,
            List<T> documents,
            ConvertDocumentToDictionary<T> documentToDictionary,
            string[]? columns = null)
        {
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;

            columns ??= documentToDictionary(documents.First()).Keys.ToArray();
            string columnList = string.Join(", ", columns);
            string placeholders = string.Join(", ", columns.Select(_ => "?"));
            command.CommandText = $"INSERT INTO {tableName} ({columnList}) VALUES ({placeholders})";
            command.Parameters.AddRange(columns.Select(_ => new OleDbParameter { Value = DBNull.Value }).ToArray());

            //foreach (var column in columns)
            //{
            //    command.Parameters.Add(new OleDbParameter { Value = DBNull.Value });
            //}
            
            try
            {
                foreach (var document in documents)
                {
                    var dictionary = documentToDictionary(document);

                    for (int j = 0; j < columns.Length; j++)
                    {
                        var column = columns[ j ];

                        var item = dictionary.TryGetValue(column, out object? value) ? value : DBNull.Value;

                        if (item is null || item is string s && string.IsNullOrWhiteSpace(s))
                        {
                            command.Parameters[ j ].Value = DBNull.Value;
                        }
                        else
                        {
                            var typeMap = item.ToAccessTypeConverter();
                            command.Parameters[ j ].Value = typeMap(item);
                        }
                    } 

                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                log.Error(ex, "Batch failed");
                throw;
            }
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

        public DataTable AddNewTableToDataSet(Type type, string tableName, string[] primaryKeys, ILogger log)
        {
            if (!TableExists(tableName))
            {
                DataTable newTable = type.ConvertObjToNewDataTable(tableName, primaryKeys);

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
            try
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
            catch (Exception ex)
            {
                log.Error(ex, "Error running query: {sql}", queryString);
            }
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
                    log.Debug("Changes committed to table {tableName}", tableName);
                }
                else
                {
                    log.Warning("Table {tableName} not found in DataSet", tableName);
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

    public class UpdateMatchRefineStatus(string dbPath, string tableName, string outputColumn) : AccessDBOperation(dbPath, tableName)
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

                    MMI_V8_Key record = GlobalHelpers.StringToObject<MMI_V8_Key>(args);

                    await matchEntityEndpoints.UpdateMatchRefineStatus(record.ExternalId); //TODO Create return types

                    row[ OutputColumn ] = "Updated";
                }
                catch (Exception ex)
                {
                    row[ OutputColumn ] = ex.Message;
                    log.Error(ex, "Error in {@args}", row.ItemArray);
                }
            }

            //CommitChanges(TableName, log);
        }
    }

    public class ResetMatchResult(string dbPath, string tableName, string outputColumn) : AccessDBOperation(dbPath, tableName)
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

                    MMI_V8_Key record = GlobalHelpers.StringToObject<MMI_V8_Key>(args);

                    await matchEntityEndpoints.ResetMatchResult(record.ExternalId); //TODO Create return types

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

            //CommitChanges(TableName, log);
        }
    }

    public class UpdateFailedFlag(string dbPath, string tableName, string outputColumn) : AccessDBOperation(dbPath, tableName)
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

                    await matchEntityEndpoints.UpdateFailedFlag(record); //TODO Create return types

                    row[ OutputColumn ] = "Updated";
                }
                catch (Exception ex)
                {
                    row[ OutputColumn ] = ex.Message;
                    log.Error(ex, "Error in {@args}", row.ItemArray);
                }
            }

            //CommitChanges(TableName, log);
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

            await base.GenerateTableFromDocuments<MatchMakeModel, MatchMakeModelRecord>(
                TableName,
                log,
                () => matchMakeModelEndpoints.GenerateMakeModelMatch(),
                (document) => [ (MatchMakeModelRecord)document ],
                (document) => GlobalHelpers.ObjToDictionary(document),
                [ nameof(MatchMakeModelRecord.MatchID) ]
                );
        }
    }

    public class PartitionMakeModelMatch(string dbPath, string tableName) : AccessDBOperation(dbPath, tableName)
    {
        private async Task<SerializableResult<List<GroupedMatchMakeModelRecord>>> GroupMatchMakeModel(ILogger log)
        {
            var matchMakeModelEndpoints = new RefitClient(log).CreateService<IMatchMakeModelEndpoints>();

            var matchMakeModelsResult = await matchMakeModelEndpoints.GetAll();

            List<MatchMakeModel> matchMakeModels = matchMakeModelsResult.Value!.Documents.Where(c => c.MMIv8Model.DocumentId is not null && c.TecDocModel.DocumentId is not null).ToList();
            int i = 1;

            Dictionary<int, List<MatchMakeModel>> GroupedMatchMakeModels = new();

            do
            {
                Dictionary<string, bool> TD_MakeModelHashes = new();
                Dictionary<string, bool> MMI_MakeModelHashes = new();

                List<MatchMakeModel> matchMakeModelsToAdd = [ matchMakeModels.First() ];

                do
                {
                    foreach (var matchMakeModel in matchMakeModelsToAdd.Distinct())
                    {
                        var newGroup = GroupedMatchMakeModels.TryAdd(i, [ matchMakeModel ]);

                        if (!newGroup)
                            GroupedMatchMakeModels[ i ].Add(matchMakeModel);
                        

                        _ = TD_MakeModelHashes.TryAdd(matchMakeModel.TecDocModel.DocumentId, false);
                        _ = MMI_MakeModelHashes.TryAdd(matchMakeModel.MMIv8Model.DocumentId, false);

                        matchMakeModels.RemoveAll(c => c.DocumentId == matchMakeModel.DocumentId);
                        //matchMakeModelsToAdd.Remove(matchMakeModel);
                    }

                    matchMakeModelsToAdd.Clear();

                    var TD_MakeModelHash = TD_MakeModelHashes.FirstOrDefault(c => !c.Value).Key;
                    if (TD_MakeModelHash != null)
                    {
                        matchMakeModelsToAdd.AddRange(matchMakeModels.Where(c => c.TecDocModel.DocumentId == TD_MakeModelHash));
                        TD_MakeModelHashes[ TD_MakeModelHash ] = true;
                    }

                    var MMI_MakeModelHash = MMI_MakeModelHashes.FirstOrDefault(c => !c.Value).Key;
                    if (MMI_MakeModelHash != null)
                    {
                        matchMakeModelsToAdd.AddRange(matchMakeModels.Where(c => c.MMIv8Model.DocumentId == MMI_MakeModelHash));
                        MMI_MakeModelHashes[ MMI_MakeModelHash ] = true;
                    }
                }
                while (TD_MakeModelHashes.Where(c => !c.Value).Any() || MMI_MakeModelHashes.Where(c => !c.Value).Any() || matchMakeModelsToAdd.Any());

                i++;
            }
            while (matchMakeModels.Any());

            return Result.Success(GroupedMatchMakeModels.SelectMany(c => c.Value.Select(d => (GroupedMatchMakeModelRecord)(c.Key, d))).ToList());
        }

        public async override Task ExecuteOperation(ILogger log)
        {
            await base.GenerateTableFromDocuments<GroupedMatchMakeModelRecord, GroupedMatchMakeModelRecord>(
                TableName,
                log,
                () => GroupMatchMakeModel(log),
                (document) => [ document ],
                (document) => GlobalHelpers.ObjToDictionary(document),
                [ nameof(GroupedMatchMakeModelRecord.MatchID) ]
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
            var matchEntityEndpoints = new RefitClient(log, 5).CreateService<IMatchEntityEndpoints>();

            await base.GenerateTableFromDocuments<MatchRefine, MatchRefine>(
                TableName,
                log,
                () => matchEntityEndpoints.GetAllMatchRefine(MakeModelMatchId, TecDocEntityId, MMIv8EntityId, IsCheck, IsMatched, IsFailed, HasDifference, Status),
                (document) => [ document ],
                (document) => GlobalHelpers.ObjToDictionary(document),
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

            await base.GenerateTableFromPagedDocuments<MatchEntitySummary, MatchEntitySummary, ObjectId>(
                TableName,
                log,
                (string? cursor, int pageSize) => matchEntityEndpoints.GetAllMatchEntitySummary(cursor, pageSize, MakeModelMatchId, TecDocEntityId, MMIv8EntityId, IsCheck, IsMatched, IsFailed, HasDifference, Status),
                (document) => [ document ],
                (document) => GlobalHelpers.ObjToDictionary(document),
                [ nameof(MatchEntitySummary.DocumentId), nameof(MatchEntitySummary.TecDocEntityId), nameof(MatchEntitySummary.MMIv8EntityId) ],
                append: Append
                );
        }
    }

    public class GenerateMatchComparisons : AccessDBOperation
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

        public GenerateMatchComparisons(string dbPath, string tableName) : base(dbPath, tableName) { }

        public GenerateMatchComparisons(
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

            await base.GenerateTableFromPagedDocuments<MatchEntity, MatchEntityComparisons, ObjectId>(
                TableName,
                log,
                (string? cursor, int pageSize) => matchEntityEndpoints.GetAll(cursor, pageSize, MakeModelMatchId, TecDocEntityId, MMIv8EntityId, IsCheck, IsMatched, IsFailed, HasDifference, Status),
                (document) 
                    => document.EntityComparison.Select(c 
                        => new MatchEntityComparisons(
                            document.DocumentId,
                            document.TecDocEntity.DocumentId,
                            document.MMIv8Entity.DocumentId,
                            document.MatchMakeModelMatchID,
                            c.Value.DocumentId,
                            c.Value.MatchBaseType,
                            c.Value.MatchBaseMethod,
                            c.Value.DefaultScore,
                            c.Value.TecDocEntity.DictToString("; "),
                            c.Value.MMIEntity.DictToString("; "),
                            c.Value.Score,
                            c.Value.Overridden,
                            c.Value.Overridden ?? false ? c.Value.MatchContexts!.Find(d => d.ContextId == c.Value.OverriddenBy)?.TecDocEntity.DictToStringWithKey() : string.Empty,
                            c.Value.Overridden ?? false ? c.Value.MatchContexts!.Find(d => d.ContextId == c.Value.OverriddenBy)?.MMIEntity.DictToStringWithKey() : string.Empty
                            )
                        ),
                (document) => GlobalHelpers.ObjToDictionary(document),
                [ nameof(MatchEntityComparisons.DocumentId), nameof(MatchEntityComparisons.MatchBaseType) ],
                pageSize: 500,
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

            await base.GenerateTableFromPagedDocuments<MatchEntity, ExpandoObject, ObjectId>(
                TableName,
                log,
                (string? cursor, int pageSize) => matchEntityEndpoints.GetAll(cursor, pageSize, MakeModelMatchId, TecDocEntityId, MMIv8EntityId, IsCheck, IsMatched, IsFailed, HasDifference, Status),
                (MatchEntity document) => [ new ExpandoObject().BuildExpando(document) ],
                (document) => (document).ToDictionary(c => c.Key, c => c.Value ?? string.Empty),
                [ nameof(MatchEntity.DocumentId) ], 
                append: Append
                );
        }
    }

    public class GenerateMatchEntityAll : IOperation
    {
        public string DBPath { get; }
        public string MatchEntityTableName { get; }
        public string MatchEntitySummaryTableName { get; }
        public string MatchEntityComparisonTableName { get; }
        public string? MakeModelMatchId { get; }
        public string? TecDocEntityId { get; }
        public string? MMIv8EntityId { get; }
        public bool? IsCheck { get; }
        public bool? IsMatched { get; }
        public bool? IsFailed { get; }
        public bool? HasDifference { get; }
        public Status[]? Status { get; }
        public bool Append { get; }

        public GenerateMatchEntityAll(
            string dbPath,
            string matchEntity,
            string matchEntitySummary,
            string matchEntityComparison,
            string? makeModelMatchId = null,
            string? tecDocEntityId = null,
            string? mmiv8EntityId = null,
            bool? isCheck = null,
            bool? isMatched = null,
            bool? isFailed = null,
            bool? hasDifference = null,
            Status[]? status = null,
            bool append = false)
        {
            DBPath = dbPath;
            MatchEntityTableName = matchEntity;
            MatchEntitySummaryTableName = matchEntitySummary;
            MatchEntityComparisonTableName = matchEntityComparison;
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

        public async Task ExecuteOperation(ILogger log)
        {
            await new GenerateMatchEntity(DBPath, MatchEntityTableName, MakeModelMatchId, TecDocEntityId, MMIv8EntityId, IsCheck, IsMatched, IsFailed, HasDifference, Status, Append).ExecuteOperation(log);
            await new GenerateMatchSummary(DBPath, MatchEntitySummaryTableName, MakeModelMatchId, TecDocEntityId, MMIv8EntityId, IsCheck, IsMatched, IsFailed, HasDifference, Status, Append).ExecuteOperation(log);
            await new GenerateMatchComparisons(DBPath, MatchEntityComparisonTableName, MakeModelMatchId, TecDocEntityId, MMIv8EntityId, IsCheck, IsMatched, IsFailed, HasDifference, Status, Append).ExecuteOperation(log);
        }
    }

    public class GenerateMatchEntityById : AccessDBOperation //TODO Tidy up expando building maybe even push to projection
    {
        public string DocumentId;
        public bool Append;
        public IMatchEntityEndpoints? MatchEntityEndpoints;

        public GenerateMatchEntityById(string dbPath, string tableName, string documentId, bool append) : base(dbPath, tableName)
        {
            DocumentId = documentId;
            Append = append;
            MatchEntityEndpoints = null;
        }
        public GenerateMatchEntityById(string dbPath, string tableName, string documentId, bool append, IMatchEntityEndpoints matchEntityEndpoints) : base(dbPath, tableName)
        {
            DocumentId = documentId;
            Append = append;
            MatchEntityEndpoints = matchEntityEndpoints;
        }

        public async override Task ExecuteOperation(ILogger log)
        {
            if (!Append)
                DropTable(TableName, log);

            MatchEntityEndpoints ??= new RefitClient(log).CreateService<IMatchEntityEndpoints>();

            var objectId = ObjectId.Parse(DocumentId);

            await base.GenerateTableFromDocument<MatchEntity, ExpandoObject>(
                TableName,
                log,
                () => MatchEntityEndpoints.GetById(objectId),
                (MatchEntity document) => [ new ExpandoObject().BuildExpando(document) ],
                (document) => (document).ToDictionary(c => c.Key, c => c.Value ?? string.Empty),
                [ nameof(MatchEntity.DocumentId) ],
                append: Append
                );
        }
    }

    public class GenerateMatchEntityByIds(string dbPath, string tableName, string outputTableName, string inputColumn, string outputColumn, bool append) : AccessDBOperation(dbPath, tableName)
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

            await base.GenerateTableFromPagedDocuments<SourceMMIv8, SourceMMIv8, ObjectId>(
                TableName,
                log,
                (string? cursor, int pageSize) => sourceEntityEndpoints.GetAll(cursor, pageSize),
                (document) => [ document ],
                (document) => GlobalHelpers.ObjToDictionary(document),
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

            await base.GenerateTableFromPagedDocuments<SourceTecDocPC, SourceTecDocPC, ObjectId>(
                TableName,
                log,
                (string? cursor, int pageSize) => sourceEntityEndpoints.GetAll(cursor, pageSize),
                (document) => [ document ],
                (document) => GlobalHelpers.ObjToDictionary(document),
                [ nameof(SourceTecDocPC.ExternalId) ],
                10000
                );
        }
    }
}
