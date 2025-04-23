using MMIv8_Ktype.Api;
using MMIv8_Ktype.Models.Util;
using Serilog;
using System.Data.OleDb;

namespace MMIv8_Ktype.AccessMdb.Operations
{
    public abstract class AccessDBOperation(string dbPath, string tableName, Serilog.ILogger Log) : IAccessDBOperation
    {
        public string DBPath { get; set; } = dbPath;
        public string TableName { get; set; } = tableName;
        
        private ILogger log = Log;
        private RefitClient refitClient = new RefitClient(Log);

        public OleDbConnection DBConnection()
        {
            var builder = new OleDbConnectionStringBuilder();

            builder.Provider = "Microsoft.ACE.OLEDB.12.0";
            builder.DataSource = DBPath;

            return new OleDbConnection(builder.ToString());
        }

        public abstract Task ExecuteOperation();

    }

    public class StorePartialMatchBase(string dbPath, string tableName, Serilog.ILogger Log) : AccessDBOperation(dbPath, tableName, Log)
    {

        public async override Task ExecuteOperation()
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



                //var matchMakeModelApi = refitClient.CreateService<IMatchMakeModelEndpoints>();

                ////var x2 = await usersClient.GetMakeModelMatch(new("003DEB088C04048C9C765F40BDF4EB28050703D366606640F7368FE93F10B7EC", "639FFB977658CC53FC74E18E5C94983B9B973EBEDDAE3B545A8F453756091CCA"));
                //var x3 = await matchMakeModelApi.GetMakeModelMatch(new("003DEB088C04048C9C765F40BDF4EB28050703D366606640F7368FE93F10B7EC", "639FFB977658CC53FC74E18E5C94983B9B973EBEDDAE3B545A8F453756091CCA"));
                //var x4 = await matchMakeModelApi.GetMakeModelMatchById(x3.MatchID);

                //await matchMakeModelApi.DeleteMakeModelMatch(x3.MatchID);
                //var x6 = await matchMakeModelApi.GetMakeModelMatch(new(x4.TecDocModel.SourceEntityModelHash, x4.MMIv8Model.SourceEntityModelHash));
                //await matchMakeModelApi.CreateMakeModelMatch(new(x4.TecDocModel.SourceEntityModelHash, x4.MMIv8Model.SourceEntityModelHash));

            }

            conn.Close();   

            conn.Dispose();
        }
    }
}
