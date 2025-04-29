using MMIv8_Ktype.AccessMdb.Maps;
using MMIv8_Ktype.Api;
using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Models;
using Serilog;
using System.Data;
using System.Data.OleDb;

namespace MMIv8_Ktype.AccessMdb.Operations
{
    public abstract class AccessDBOperation(string dBPath, string tableName) : IAccessDBOperation
    {
        public string DBPath { get; set; } = dBPath;
        public string TableName { get; set; } = tableName;

        internal string QueryString = $"SELECT * FROM [{tableName}]";
        internal DataSet DBDataSet = new();

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
            IMatchBaseEndpoints matchBaseEndpoints = new RefitClient(log).CreateService<IMatchBaseEndpoints>();

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

    public class GenerateMakeModelMatch(string dbPath, string tableName) : AccessDBOperation(dbPath, tableName)
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            DropTable(TableName, log);

            var matchMakeModels = await new RefitClient(log).CreateService<IMatchMakeModelEndpoints>().GenerateMakeModelMatch();

            MatchMakeModelRecord matchMakeModels1 = matchMakeModels.First();

            DataTable dataTable = AddNewTableToDataSet(matchMakeModels1, TableName, [ "MatchID" ], log) ?? throw new NoNullAllowedException();


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

            log.Information("Loaded {tableCount} / {apicount} records into {table}", dataTable.Rows.Count, matchMakeModels.Count, TableName );
        }
    }
}
