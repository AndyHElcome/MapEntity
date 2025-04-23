using MMIv8_Ktype.Models;
using Serilog.Events;
using Serilog;
using System.Reflection;
using MMIv8_Ktype.Api;
using MMIv8_Ktype.Api.Endpoints;

internal class Program
{
    static async Task Main(string[] args)
    {
        Serilog.ILogger Log = new LoggerConfiguration() // TODO put into application settings
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: "[{Level:u3}] {Message:l}{NewLine}{Exception}")
            .WriteTo.File($"..\\..\\..\\..\\MMIv8_Ktype.Console\\Logs\\Log_Console.txt",
                            rollOnFileSizeLimit: true,
                            fileSizeLimitBytes: 1048576,
                            shared: true,
                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:l}{NewLine}{Exception}",
                            restrictedToMinimumLevel: LogEventLevel.Information)
            .CreateLogger();

        //EXAMPLE
        //var refitClient = new RefitClient(Log);

        //var matchMakeModelApi = refitClient.CreateService<IMatchMakeModelEndpoints>();

        ////var x2 = await usersClient.GetMakeModelMatch(new("003DEB088C04048C9C765F40BDF4EB28050703D366606640F7368FE93F10B7EC", "639FFB977658CC53FC74E18E5C94983B9B973EBEDDAE3B545A8F453756091CCA"));
        //var x3 = await matchMakeModelApi.GetMakeModelMatch(new("003DEB088C04048C9C765F40BDF4EB28050703D366606640F7368FE93F10B7EC", "639FFB977658CC53FC74E18E5C94983B9B973EBEDDAE3B545A8F453756091CCA"));
        //var x4 = await matchMakeModelApi.GetMakeModelMatchById(x3.MatchID);

        //await matchMakeModelApi.DeleteMakeModelMatch(x3.MatchID);
        //var x6 = await matchMakeModelApi.GetMakeModelMatch(new(x4.TecDocModel.SourceEntityModelHash, x4.MMIv8Model.SourceEntityModelHash));
        //await matchMakeModelApi.CreateMakeModelMatch(new(x4.TecDocModel.SourceEntityModelHash, x4.MMIv8Model.SourceEntityModelHash));

       

        args = [ "MMIv8_Ktype.AccessMdb", "StorePartialMatchBase", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\MMIv8_Ktype2.accdb", "API_StorePartialMatchBase" ];

        if (args.Length == 0)
        {
            Console.WriteLine("Usage: <ClassName> <ConstructorArgs...>");
            return;
        }

        string library = args[ 0 ];
        string className = args[ 1 ];
        string[] constructorArgs = args.Skip(2).ToArray();

        try
        {
            Type? type = Assembly.Load(new AssemblyName(library))
                                 .GetTypes()
                                 .FirstOrDefault(t => t.Name.Equals(className, StringComparison.OrdinalIgnoreCase) && t.Namespace.StartsWith($"{library}.Operations"));

            if (type == null)
            {
                Log.Error("Class '{className}' not found.", className);
                return;
            }

            // Find a constructor that matches the number of parameters
            var constructor = type.GetConstructors()
                                  .FirstOrDefault(c => c.GetParameters().Length == constructorArgs.Length);

            if (constructor == null)
            {

                Log.Error("No matching constructor found for class '{className}' with {constructorArgs.Length} parameters.", className, constructorArgs.Length);
                //Console.WriteLine($"No matching constructor found for class '{className}' with {constructorArgs.Length} parameters.");
                return;
            }

            // Convert parameters to the constructor parameter types
            var parameters = constructor.GetParameters();
            object[] parsedArgs = new object[ constructorArgs.Length ];
            for (int i = 0; i < constructorArgs.Length; i++)
            {
                parsedArgs[ i ] = Convert.ChangeType(constructorArgs[ i ], parameters[ i ].ParameterType);
            }

            // Instantiate the class
            IOperation instance = (IOperation)constructor.Invoke(parsedArgs);

            Log.Information("Instance of {className} created successfully.", className);

            await instance.ExecuteOperation();
        }
        catch (Exception ex)
        {
            Log.Error("Error: {ex}", ex.Message);
            //Console.WriteLine($"Error: {ex.Message}");
        }
    }
}