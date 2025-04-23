using MMIv8_Ktype.Api;
using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Models.Util;
using Serilog;
using System.Data.OleDb;

namespace MMIv8_Ktype.AccessMdb.Operations
{
    public abstract class AccessDBOperation(string dbPath, string tableName) : IAccessDBOperation
    {
        public string DBPath { get; set; } = dbPath;
        public string TableName { get; set; } = tableName;

        public abstract Task ExecuteOperation(ILogger log);

        public OleDbConnection DBConnection()
        {
            var builder = new OleDbConnectionStringBuilder();

            builder.Provider = "Microsoft.ACE.OLEDB.12.0";
            builder.DataSource = DBPath;

            return new OleDbConnection(builder.ToString());
        }
    }

    public class TestAccessDBOperation(string dbPath, string tableName) : AccessDBOperation(dbPath, tableName)
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            var refitClient = new RefitClient(log);
            // Version Provider
            var versionProvider = refitClient.VersionProvider;

            var versionEndpoints = refitClient.CreateService<IVersionEndpoints>();

            var currentVersion = await versionEndpoints.GetCurrentVersion();

            var matchMakeModelEndpoints = refitClient.CreateService<IMatchMakeModelEndpoints>();

            var matchMakeModel = matchMakeModelEndpoints.GenerateModelMatch();

            using var conn = DBConnection();

            var matchBaseEndpoints = refitClient.CreateService<IMatchBaseEndpoints>();

            conn.Open();
            string query = $"SELECT * FROM [{TableName}]";

            Log.Information("SQL Query {query}", query);

            using var cmd = new OleDbCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var matchBaseType = (MatchBaseType)Enum.Parse(typeof(MatchBaseType), reader[ "MatchBaseType" ]?.ToString() ?? throw new NullReferenceException("Unexpected null"), true);
                var matchHash = reader[ "MatchHash" ]?.ToString() ?? throw new NullReferenceException("Unexpected null");
                var newScore = Convert.ToDecimal( reader[ "NewScore" ] ?? throw new NullReferenceException("Unexpected null") ) ;

                await matchBaseEndpoints.StorePartialMatchBase(new Api.Requests.PutMatchBaseRequest(matchBaseType, matchHash, newScore));
            }

            conn.Close();   

            conn.Dispose();
        }
    }

    public class StorePartialMatchBase(string dbPath, string tableName) : AccessDBOperation(dbPath, tableName)
    {
        public async override Task ExecuteOperation(ILogger log)
        {
            using var conn = DBConnection();

            var matchBaseEndpoints = new RefitClient(log).CreateService<IMatchBaseEndpoints>();

            conn.Open();
            string query = $"SELECT * FROM [{TableName}]";

            Log.Information("SQL Query {query}", query);

            using var cmd = new OleDbCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var matchBaseType = (MatchBaseType)Enum.Parse(typeof(MatchBaseType), reader[ "MatchBaseType" ]?.ToString() ?? throw new NullReferenceException("Unexpected null"), true);
                var matchHash = reader[ "MatchHash" ]?.ToString() ?? throw new NullReferenceException("Unexpected null");
                var newScore = Convert.ToDecimal( reader[ "NewScore" ] ?? throw new NullReferenceException("Unexpected null") ) ;

                await matchBaseEndpoints.StorePartialMatchBase(new Api.Requests.PutMatchBaseRequest(matchBaseType, matchHash, newScore));
            }

            conn.Close();   

            conn.Dispose();
        }
    }

}
