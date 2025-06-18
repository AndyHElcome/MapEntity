using Serilog.Events;
using Serilog;
using System.Reflection;
using MMIv8_Ktype.Api;
using MMIv8_Ktype.Models;
using System.Diagnostics;

internal class Program
{    
    public static class Logger
    {
        public static ILogger Log = SetupLogger("D:\\GIT\\MMIv8_Ktype\\MMIv8_Ktype.Console\\Logs");
        public static ILogger SetupLogger(string filePath)
        {
#if DEBUG
            filePath = $"..\\..\\..\\..\\MMIv8_Ktype.Console\\Logs\\Log_Console.txt";
#endif

            return new LoggerConfiguration() // TODO put into application settings
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: "[{Level:u3}] {Message:l}{NewLine}{Exception}")
                .WriteTo.File(filePath,
                                rollOnFileSizeLimit: true,
                                fileSizeLimitBytes: 1048576,
                                shared: true,
                                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:l}{NewLine}{Exception}",
                                restrictedToMinimumLevel: LogEventLevel.Information)
                .CreateLogger();
        }
    }

    static async Task Main(string?[] args)
    {
        var Log = Logger.Log;


#if DEBUG
        //args = [ "MMIv8_Ktype.AccessMdb", "StorePartialMatchBase", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\MMIv8_Ktype_Data.accdb", "API_StorePartialMatchBase", "ApiResponse" ];

        //args = [ "MMIv8_Ktype.AccessMdb", "GenerateMakeModelMatch", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\MMIv8_Ktype_Data.accdb", "MatchMakeModel" ];

        args = [ "MMIv8_Ktype.AccessMdb", "PartitionMakeModelMatch", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\MMIv8_Ktype_Data.accdb", "GroupedMatchMakeModel" ];

        //args = [ "MMIv8_Ktype.AccessMdb", "LoadPreviousMatches", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\MMIv8_Ktype_Data.accdb", "LegacyMasterlist", "1" ];

        //args = [ "MMIv8_Ktype.AccessMdb", "GenerateMatchRefine", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\MMIv8_Ktype_Data.accdb", "MatchRefine" ];

        //args = [ "MMIv8_Ktype.AccessMdb", "GenerateMatchRefine", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\MMIv8_Ktype_Data.accdb", "MatchRefine", "null", "null", "null", "true", "null", "null", "null", "null", "false" ];

        //args = [ "MMIv8_Ktype.AccessMdb", "GenerateMatchSummary", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\MMIv8_Ktype_Data.accdb", "MatchEntitySummary" ];

        //args = [ "MMIv8_Ktype.AccessMdb", "GenerateMatchSummary", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\MMIv8_Ktype_Data.accdb", "MatchEntitySummary", "6849667ac745bff32b4bfce4", "null", "null", "null", "null", "null", "null", "null", "true" ];

        //args = [ "MMIv8_Ktype.AccessMdb", "GenerateMatchEntity", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\MMIv8_Ktype_Data.accdb", "MatchEntity" ];

        //args = [ "MMIv8_Ktype.AccessMdb", "GenerateMatchEntity", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\MMIv8_Ktype_Data.accdb", "MatchEntity", "68496657c745bff32b4a6df9", "null", "null", "null", "null", "null", "null", "null", "true" ];

        //args = [ "MMIv8_Ktype.AccessMdb", "GenerateMatchComparisons", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\MMIv8_Ktype_Data.accdb", "MatchEntityComparisons", "6849667ac745bff32b4bfce4", "null", "null", "null", "null", "false", "null", "null", "true" ];

        args = [ "MMIv8_Ktype.AccessMdb", "GenerateMatchEntityAll", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\MMIv8_Ktype_Data.accdb", "MatchEntity", "MatchEntitySummary", "MatchEntityComparisons", "6849639dc745bff32b355a27", "null", "null", "null", "null", "null", "null", "null", "true" ];

        //args = [ "MMIv8_Ktype.AccessMdb", "GenerateMatchEntityById", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\MMIv8_Ktype_Data.accdb", "MatchEntity", "68387b61db32859096fa61f4", "true" ];

        //args = [ "MMIv8_Ktype.AccessMdb", "GenerateMatchEntityByIds", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\MMIv8_Ktype_Data.accdb", "MatchIdsToGenerate", "MatchEntity", "input", "output", "false" ];

        //args = [ "MMIv8_Ktype.AccessMdb", "UpdateMatchedFlag", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\MMIv8_Ktype_Data.accdb", "UpdateMatchFlag", "output" ];

        //args = [ "MMIv8_Ktype.AccessMdb", "UpdateMatchRefineStatus", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\MMIv8_Ktype_Data.accdb", "UpdateMatchRefineStatus", "output" ];

        //args = [ "MMIv8_Ktype.AccessMdb", "ResetMatchResult", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\MMIv8_Ktype_Data.accdb", "UpdateMatchRefineStatus", "output" ];

        //args = [ "MMIv8_Ktype.AccessMdb", "GenerateMMIEntities", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\MMIv8_Ktype_Data.accdb", "Source_MMIv8" ];

        //args = [ "MMIv8_Ktype.AccessMdb", "GenerateTecDocEntities", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\MMIv8_Ktype_Data.accdb", "Source_TecDocPC" ];

        //args = [ "MMIv8_Ktype.CSV", "GenerateModelMatchCSV", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\Output\MakeModelMatch.csv" ];

        //args = [ "MMIv8_Ktype.CSV", "CSVBackupOperation", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\Backup" ];

        //args = [ "MMIv8_Ktype.CSV", "CSVBackupInitialise", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\Backup", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\Source_MMIv8.txt", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\Source_TD_PC.txt", "PreviousRelations.csv" ];

        //args = [ "MMIv8_Ktype.CSV", "ImportMakeModelMatch", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\Backup\MatchMakeModel.csv" ];

        //args = [ "MMIv8_Ktype.CSV", "UpdateMatchBaseScore", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\Backup\MatchFuel.csv" ];

        //args = [ "MMIv8_Ktype.CSV", "StorePartialMatchBase", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\Backup\MatchIdentifier.csv" ];

        //args = [ "MMIv8_Ktype.CSV", "LoadEntityRelation", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\Backup\PreviousRelations.csv", "1" ];

        //args = [ "MMIv8_Ktype.CSV", "UpdateTecDocEntity", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\Source_TD_PC-124.txt" ];

        //args = [ "MMIv8_Ktype.CSV", "UpdateMMIv8Entity", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\Source_MMIv8-100051.txt" ];

        //args = [ "MMIv8_Ktype.CSV", "ReloadTecDocPCEntities", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\Source_TD_PC.txt" ];

        //args = [ "MMIv8_Ktype.CSV", "ReloadMMIv8Entities", @"C:\Users\andy.hargreaves\OneDrive - Elcome Ltd\Desktop\NEW MMI TO KTYPE\Source_MMIv8.txt" ];
#endif

        if (args.Length < 2)
        {
            Log.Error("No args parsed. Usage: <Library> <ClassName> <ConstructorArgs...>");
            return;
        }

        var sw = Stopwatch.StartNew();

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

            Log.Information("Instance of {className} created successfully. {time}", className, sw);

            await instance.ExecuteOperation(Log);

            Log.Information("Instance of {className} completed. {time}", className, sw);

#if DEBUG
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
#endif
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error running command: {@args} {time}", args, sw);
        }
    }
}