using MMIv8_Ktype.Models;
using Serilog.Events;
using Serilog;
using System.Reflection;
using MMIv8_Ktype.Api;
using MongoDB.Bson;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json;
using Refit;
using MMIv8_Ktype.Models.ApiServices;
using MMIv8_Ktype.Models.Endpoints;

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

        var usersClient = RestService.For<IMatchMakeModelApiService>("https://localhost:44304");

        var test = EndpointExtensions.Refit<IMatchMakeModelApiService>();

        //var x2 = await usersClient.GetMakeModelMatch(new("003DEB088C04048C9C765F40BDF4EB28050703D366606640F7368FE93F10B7EC", "639FFB977658CC53FC74E18E5C94983B9B973EBEDDAE3B545A8F453756091CCA"));
        var x3 = await test.GetMakeModelMatch(new("003DEB088C04048C9C765F40BDF4EB28050703D366606640F7368FE93F10B7EC", "639FFB977658CC53FC74E18E5C94983B9B973EBEDDAE3B545A8F453756091CCA"));
        var x4 = await test.GetMakeModelMatchById(x3.MatchID);

        using var httpClient = new HttpClient(); // TODO put into application settings
        //t.DefaultRequestHeaders.Add("Authorization", "");
        //t.DefaultRequestHeaders.Add("User-Agent", "");
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
        httpClient.BaseAddress = new Uri("https://localhost:44304");

        var mmiv8_KtypeService = new MMIv8_KtypeService(httpClient, Log);

        var t = await mmiv8_KtypeService.GetCurrentVersion();
        var y = await mmiv8_KtypeService.GetMakeModelMatch(new("003DEB088C04048C9C765F40BDF4EB28050703D366606640F7368FE93F10B7EC", "639FFB977658CC53FC74E18E5C94983B9B973EBEDDAE3B545A8F453756091CCA"));
        var z = await mmiv8_KtypeService.GetMakeModelMatchById(y.MatchID);

        var x = await mmiv8_KtypeService.DeleteMakeModelMatch(y.MatchID);
        var f = await mmiv8_KtypeService.CreateMakeModelMatch(new("003DEB088C04048C9C765F40BDF4EB28050703D366606640F7368FE93F10B7EC", "639FFB977658CC53FC74E18E5C94983B9B973EBEDDAE3B545A8F453756091CCA"));



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

            await instance.ExecuteOperation(Log);
        }
        catch (Exception ex)
        {
            Log.Error("Error: {ex}", ex.Message);
            //Console.WriteLine($"Error: {ex.Message}");
        }
    }
}