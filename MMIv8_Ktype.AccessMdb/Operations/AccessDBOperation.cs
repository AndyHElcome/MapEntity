using MMIv8_Ktype.AccessMdb.Maps;
using MMIv8_Ktype.Api;
using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Api.Responses;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Outputs;
using MMIv8_Ktype.Models.Util;
using Serilog;
using System.Data;
using System.Data.OleDb;
using System.Dynamic;
using System.Formats.Asn1;
using System.Text.Json;
using System.Text.Json.Nodes;

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

    public class TestAccessDBOperation(string dbPath, string tableName, string outputColumn) : AccessDBOperation(dbPath, tableName)
    {
        public string OutputColumn = outputColumn;

        public async override Task ExecuteOperation(ILogger log)
        {

            var refitClient = new RefitClient(log);
            //// Version Provider
            //var versionProvider = refitClient.VersionProvider;

            //var versionEndpoints = refitClient.CreateService<IVersionEndpoints>();

            //var currentVersion = await versionEndpoints.GetCurrentVersion();

            //var matchMakeModelEndpoints = refitClient.CreateService<IMatchMakeModelEndpoints>();

            //var matchMakeModel = matchMakeModelEndpoints.GenerateModelMatch();


            var matchBaseEndpoints = refitClient.CreateService<IMatchBaseEndpoints>();

            DataTable dataTable = DBDataSet.Tables[ TableName ] ?? throw new NoNullAllowedException();

            foreach (DataRow row in dataTable.Rows)
            {
                string[] args = row.ItemArray.Select(c => c!.ToString() ?? string.Empty).Take(3).ToArray();

                var putMatchBaseRequest = GlobalHelpers.StringToObject<PutMatchBaseRequest>(args);

                await matchBaseEndpoints.StorePartialMatchBase(putMatchBaseRequest);

                row[ OutputColumn ] = "Updated";
            }

            CommitChanges(TableName, log);
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

                    await matchBaseEndpoints.StorePartialMatchBase(putMatchBaseRequest); //TODO Create return types

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
            DropTable(TableName, log);

            var matchMakeModels = await new RefitClient(log).CreateService<IMatchMakeModelEndpoints>().GenerateMakeModelMatch();

            MatchMakeModelRecord matchMakeModels1 = matchMakeModels.First(); //TODO Change this

            DataTable dataTable = AddNewTableToDataSet(matchMakeModels1, TableName, [ nameof(MatchMakeModelRecord.MatchID) ], log) ?? throw new NoNullAllowedException();

            foreach (MatchMakeModelRecord matchMakeModel in matchMakeModels)
            {
                try
                {
                    var newRow = dataTable.NewRow().ConvertObjToDataRow(matchMakeModel);

                    dataTable.Rows.Add(newRow);
                }
                catch (Exception ex)
                {
                    log.Error(ex, "Error writing {@item}", matchMakeModel);
                }
            }

            CommitChanges(TableName, log);

            log.Information("Loaded {tableCount} / {apicount} records into {table}", dataTable.Rows.Count, matchMakeModels.Count, TableName);
        }
    }

    public class GenerateMatchRefine(string dbPath, string tableName) : AccessDBOperation(dbPath, tableName)//TODO FIX FOR MATCH REFINE
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            DropTable(TableName, log);

            var matchEntityEndpoints = new RefitClient(log).CreateService<IMatchEntityEndpoints>();

            var response = await matchEntityEndpoints.GetAllMatchRefine();

            var firstRecord = response.FirstOrDefault(); //TODO Change this

            if (firstRecord is null)
                return;

            DataTable dataTable = AddNewTableToDataSet(firstRecord, TableName, [ nameof(MatchRefine.MMIv8EntityId) ], log) ?? throw new NoNullAllowedException();

            foreach (MatchRefine matchEntity in response)
            {
                try
                {
                    var newRow = dataTable.NewRow().ConvertObjToDataRow(matchEntity);
                    dataTable.Rows.Add(newRow);
                }
                catch (Exception ex)
                {
                    log.Error(ex, "Error writing {@item}", matchEntity);
                }
            }

            CommitChanges(TableName, log);

            log.Information("Loaded {tableCount} / {apicount} records into {table}", dataTable.Rows.Count, response.Count, TableName);
        }
    }

    public class GenerateMatchSummary(string dbPath, string tableName) : AccessDBOperation(dbPath, tableName)
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            DropTable(TableName, log);

            var matchEntityEndpoints = new RefitClient(log).CreateService<IMatchEntityEndpoints>();

            var singleMatch = await matchEntityEndpoints.GetAllMatchEntitySummary(1, 1, MakeModelMatchId: "682484cac744b4c72aec79a5");
            var firstRecord = singleMatch.Documents.FirstOrDefault(); //TODO Change this

            if (firstRecord is null)
                return;

            DataTable dataTable = AddNewTableToDataSet(firstRecord, TableName, [ nameof(MatchEntitySummary.DocumentId) ], log) ?? throw new NoNullAllowedException();

            int page = 1;
            PagedResponse<MatchEntitySummary> response;

            do
            {
                response = await matchEntityEndpoints.GetAllMatchEntitySummary(page, 1000, MakeModelMatchId: "682484cac744b4c72aec79a5");
                foreach (MatchEntitySummary matchEntity in response.Documents)
                {
                    try
                    {
                        var newRow = dataTable.NewRow().ConvertObjToDataRow(matchEntity);
                        dataTable.Rows.Add(newRow);
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex, "Error writing {@item}", matchEntity);
                    }
                }
                page++;
            }
            while (response.HasNextPage);

            CommitChanges(TableName, log);

            log.Information("Loaded {tableCount} / {apicount} records into {table}", dataTable.Rows.Count, response.TotalDocuments, TableName);
        }
    }

    public class GenerateMatchEntity(string dbPath, string tableName) : AccessDBOperation(dbPath, tableName) //TODO Tidy up expando building maybe even push to project
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            DropTable(TableName, log);

            var matchEntityEndpoints = new RefitClient(log).CreateService<IMatchEntityEndpoints>();
            var t = await matchEntityEndpoints.GetAll(1, 1, MakeModelMatchId: "682484cac744b4c72aec79a5");
            var firstRecord = t.Documents.FirstOrDefault(); //TODO Change this

            var expando = new ExpandoObject();
            expando.BuildExpando(firstRecord);

            var dict = expando.ToDictionary();

            if (dict is null)
                return;

            DataTable dataTable = AddNewTableToDataSet(dict, TableName, [ nameof(MatchEntity.DocumentId) ], log) ?? throw new NoNullAllowedException();

            int page = 1;
            PagedResponse<MatchEntity> response;

            do
            {
                response = await matchEntityEndpoints.GetAll(page, 1000, MakeModelMatchId: "682484cac744b4c72aec79a5");
                foreach (MatchEntity matchEntity in response.Documents)
                {
                    try
                    {
                        expando = new ExpandoObject();
                        expando.BuildExpando(matchEntity);
                        dict = expando.ToDictionary();

                        var newRow = dataTable.NewRow().ConvertObjToDataRow(dict);
                        dataTable.Rows.Add(newRow);
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex, "Error writing {@item}", dict);
                    }
                }
                page++;
            }
            while (response.HasNextPage);

            CommitChanges(TableName, log);

            log.Information("Loaded {tableCount} / {apicount} records into {table}", dataTable.Rows.Count, response.TotalDocuments, TableName);
        }
    }

    public class GenerateMMIEntities(string dbPath, string tableName) : AccessDBOperation(dbPath, tableName)
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            DropTable(TableName, log);

            var sourceEntityEndpoints = new RefitClient(log).CreateService<ISourceMMIv8Endpoints>();

            var singleMatch = await sourceEntityEndpoints.GetAll(1, 1);
            var firstRecord = singleMatch.Documents.FirstOrDefault(); //TODO Change this

            if (firstRecord is null)
                return;

            DataTable dataTable = AddNewTableToDataSet(new SourceMMIv8(), TableName, [ nameof(SourceMMIv8.ExternalId) ], log) ?? throw new NoNullAllowedException();

            int page = 1;
            PagedResponse<SourceMMIv8> response;

            do
            {
                response = await sourceEntityEndpoints.GetAll(page, 1000);
                foreach (SourceMMIv8 matchEntity in response.Documents)
                {
                    try
                    {
                        var newRow = dataTable.NewRow().ConvertObjToDataRow(matchEntity);
                        dataTable.Rows.Add(newRow);
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex, "Error writing {@item}", matchEntity);
                    }
                }
                page++;
            }
            while (response.HasNextPage);

            CommitChanges(TableName, log);

            log.Information("Loaded {tableCount} / {apicount} records into {table}", dataTable.Rows.Count, response.TotalDocuments, TableName);
        }
    }
}
