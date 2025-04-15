using MongoDB.Driver;
using MongoDB.Bson;
using Serilog;

namespace MMIv8_Ktype.Core.Contexts
{
    public class MongoDBContext
    {
        public MongoDBContext(IConfiguration configuration)
        {
            Configuration = configuration;
            Client = CreateClient();
            MongoDatabase = CreateConnection();
            Collections = new MongoDBCollections(Configuration, MongoDatabase);
        }

        //const string connectionUri = "mongodb+srv://AndyH:s2mQ2bw8cknbs89M@cluster0.bldu0.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0";
        const string connectionUri = "mongodb://admin:elcome_b055@10.0.0.10:27017/?retryWrites=true&w=majority";

        protected readonly IConfiguration Configuration;
        protected readonly IMongoDatabase MongoDatabase;
        public MongoClient Client { get; }
        public MongoDBCollections Collections { get; }

        public MongoClient CreateClient()
        {
            var settings = MongoClientSettings.FromConnectionString(connectionUri);

            //settings.LinqProvider = LinqProvider.V3;
            // Set the ServerApi field of the settings object to set the version of the Stable API on the client
            settings.ServerApi = new ServerApi(ServerApiVersion.V1);

            // Create a new client and connect to the server
            return new MongoClient(settings);
        }

        public IMongoDatabase CreateConnection()
        {
            try
            {
                var database = Client.GetDatabase("MMIv8_Ktype");
                var result = database.RunCommand<BsonDocument>(new BsonDocument("ping", 1));
                Log.Information("Pinged your deployment. You successfully connected to MongoDB!");

                return database;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during CreateConnection");
                throw;
            }
        }
    }
}
