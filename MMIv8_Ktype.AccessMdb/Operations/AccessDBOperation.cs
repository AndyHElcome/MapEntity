using DnsClient.Internal;
using MMIv8_Ktype.Models.Util;
using System.Data.OleDb;

namespace MMIv8_Ktype.AccessMdb.Operations
{
    public abstract class AccessDBOperation(string dbPath, string tableName) : IAccessDBOperation
    {
        public string DBPath { get; set; } = dbPath;
        public string TableName { get; set; } = tableName;
        public OleDbConnection DBConnection()
        {
            var builder = new OleDbConnectionStringBuilder();

            builder.Provider = "Microsoft.ACE.OLEDB.12.0";
            builder.DataSource = DBPath;

            return new OleDbConnection(builder.ToString());
        }

        public abstract Task ExecuteOperation(Serilog.ILogger Log);
    }

    public class StorePartialMatchBase(string dbPath, string tableName) : AccessDBOperation(dbPath, tableName)
    {

        public async override Task ExecuteOperation(Serilog.ILogger Log)
        {
            using var conn = DBConnection();

            conn.Open();
            string query = $"SELECT * FROM [{TableName}]";

            Log.Information("SQL Query {query}", query);

            using var cmd = new OleDbCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var matchBaseType = (MatchBaseType)Enum.Parse(typeof(MatchBaseType), reader[ "MatchBaseType" ]?.ToString() ?? throw new NullReferenceException("Unexpected null"), true);
                var matchHash = reader[ "MatchHash" ]?.ToString() ?? throw new NullReferenceException("Unexpected null");
                var newScore = reader[ "NewScore" ]?.ToString() ?? throw new NullReferenceException("Unexpected null");


                //var storePartialMatchBase = new Api.StorePartialMatchBase(
                //    matchBaseType,
                //    matchHash,
                //    Convert.ToDecimal(newScore)
                //    );


                //var r = await storePartialMatchBase.MakeCall(Log);

            }

            conn.Close();   

            conn.Dispose();
        }
    }
}
