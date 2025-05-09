using Serilog.Events;
using Serilog;
using System.Reflection;
using MMIv8_Ktype.Api;
using MMIv8_Ktype.Models;

internal class Program
{    
    public static class Logger
    {
        public static ILogger Log = SetupLogger();
        public static ILogger SetupLogger()
        {
            return new LoggerConfiguration() // TODO put into application settings
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: "[{Level:u3}] {Message:l}{NewLine}{Exception}")
                .WriteTo.File($"..\\..\\..\\..\\MMIv8_Ktype.Console\\Logs\\Log_Console.txt",
                                rollOnFileSizeLimit: true,
                                fileSizeLimitBytes: 1048576,
                                shared: true,
                                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:l}{NewLine}{Exception}",
                                restrictedToMinimumLevel: LogEventLevel.Information)
                .CreateLogger();
        }
    }

    static async Task Main(string[] args)
    {
        var Log = Logger.Log;

        //args = [ "MMIv8_Ktype.AccessMdb", "TestAccessDBOperation", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\MMIv8_Ktype2.accdb", "API_StorePartialMatchBase", "ApiResponse" ];

        //args = [ "MMIv8_Ktype.AccessMdb", "StorePartialMatchBase", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\MMIv8_Ktype2.accdb", "API_StorePartialMatchBase", "ApiResponse" ];

        //args = [ "MMIv8_Ktype.AccessMdb", "GenerateMakeModelMatch", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\MMIv8_Ktype2.accdb", "MatchMakeModel" ];

        //args = [ "MMIv8_Ktype.AccessMdb", "LoadPreviousMatches", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\MMIv8_Ktype2.accdb", "LegacyMasterlist", "1" ];

        //args = [ "MMIv8_Ktype.CSV", "TestCsvReadOperation", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\Output\MakeModelMatch.csv" ];

        //args = [ "MMIv8_Ktype.CSV", "TestCsvWriteOperation", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\Output\MakeModelMatch.csv" ];

        //args = [ "MMIv8_Ktype.CSV", "GenerateModelMatchCSV", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\Output\MakeModelMatch.csv" ];

        //args = [ "MMIv8_Ktype.CSV", "CSVBackupOperation", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\Backup" ];

        args = [ "MMIv8_Ktype.CSV", "CSVBackupInitialise", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\Backup", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\Source_MMIv8.txt", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\Source_TD_PC.txt", "PreviousRelations.csv" ];

        //args = [ "MMIv8_Ktype.CSV", "ImportMakeModelMatch", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\Backup\MatchMakeModel.csv" ];

        args = [ "MMIv8_Ktype.CSV", "UpdateMatchBaseScore", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\Backup\MatchDrive.csv" ];

        //args = [ "MMIv8_Ktype.CSV", "StorePartialMatchBase", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\Backup\MatchIdentifier.csv" ];

        //args = [ "MMIv8_Ktype.CSV", "LoadEntityRelation", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\Backup\PreviousRelations.csv", "1" ];

        if (args.Length < 2)
        {
            Log.Error("No args parsed. Usage: <Library> <ClassName> <ConstructorArgs...>");
            return;
        }

        string library = args[ 0 ];
        string className = args[ 1 ];
        string[] constructorArgs = args.Skip(2).ToArray();



        try
        {
            var assemblyName = new AssemblyName(library);
            var availableTypes = Assembly.Load(assemblyName)
                                            .GetTypes()
                                            .Where(t => !t.IsAbstract && !t.IsInterface && t.IsAssignableTo(typeof(IOperation)));
            //Get Operation Type
            var type = availableTypes.FirstOrDefault(t => t.Name.Equals(className, StringComparison.OrdinalIgnoreCase));

            if (type == null)
            {
                Log.Error("Try one of the following {@types}", availableTypes.Select(c => c.Name));
                throw new Exception($"Class '{className}' of '{nameof(IOperation)}' not found in '{library}'");
            }

            IOperation instance = type.StringToObject<IOperation>(constructorArgs);

            Log.Information("Instance of {className} created successfully.", className);

            await instance.ExecuteOperation(Log);

            Log.Information("Instance of {className} completed.", className);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error running command: {@args}", args);
        }
    }
}