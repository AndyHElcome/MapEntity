using MMIv8_Ktype.Models;
using Serilog.Events;
using Serilog;
using System.Reflection;
using MMIv8_Ktype.Api;

internal class Program
{
    static async Task Main(string[] args)
    {
        using var httpClient = new HttpClient();
        //t.DefaultRequestHeaders.Add("Authorization", "");
        //t.DefaultRequestHeaders.Add("User-Agent", "");
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
        httpClient.BaseAddress = new Uri("https://localhost:44304");

        var mmiv8_KtypeService = new MMIv8_KtypeService(httpClient);

        var t = mmiv8_KtypeService.GetCurrentVersion();

        Serilog.ILogger Log = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console(outputTemplate: "[{Level:u3}] {Message:l}{NewLine}{Exception}")
        .WriteTo.File($"..\\..\\..\\..\\MMIv8_Ktype.Console\\Logs\\Log_Console.txt",
                        rollOnFileSizeLimit: true,
                        fileSizeLimitBytes: 1048576,
                        shared: true,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:l}{NewLine}{Exception}",
                        restrictedToMinimumLevel: LogEventLevel.Information)
        .CreateLogger();

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