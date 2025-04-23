using CsvHelper;
using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Core.Services.Source;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Models.Util;
using MongoDB.Driver;
using Serilog;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace MMIv8_Ktype.Core.Services.Mapping
{
    public class BulkMappingService(MongoDBContext MMIv8_Ktype,
                                    MappingService MappingService,
                                    SourceMMIv8EntityModelService SourceMMIv8EntityModelService,
                                    SourceTecDocEntityModelService SourceTecDocEntityModelService,
                                    MatchMakeModelService MatchMakeModelService,
                                    MatchEntityService MatchEntityService,
                                    MatchEntityContext MatchEntityContext,
                                    MatchBaseService MatchBaseService,
                                    ISourceEntityService<MongoSourceMMIv8> SourceMMIv8Service,
                                    ISourceEntityService<MongoSourceTecDocPC> SourceTecDocPCService,
                                    IVersionProvider versionProvider) 
    {
        #region Match Make Model

        public async Task<List<MatchMakeModel>> GenerateModelMatch()
        {
            List<MatchMakeModel> currentMatch = (await MatchMakeModelService.GetAll()).ToList();

            var MMIv8Models = await SourceMMIv8EntityModelService.GetByNotId(currentMatch.Select(m => m.MMIv8Model.SourceEntityModelHash).ToArray());

            var MMINoMatches = MMIv8Models.ToList().Select(m =>
                new MatchMakeModel(tecDocModel: new(), mmiv8Model: m, versionProvider)).ToList();


            var TecDocPCModels = await SourceTecDocEntityModelService.GetByNotId(currentMatch.Select(m => m.TecDocModel.SourceEntityModelHash).ToArray());

            var TecDocNoMatches = TecDocPCModels.ToList().Select(m =>
                new MatchMakeModel(tecDocModel: m, mmiv8Model: new(), versionProvider)).ToList();

            List<MatchMakeModel> matchMakeModels = currentMatch
                                                        .Union(MMINoMatches)
                                                        .Union(TecDocNoMatches)
                                                        .GroupBy(i => new { mmiHash = i.MMIv8Model.SourceEntityModelHash, tdHash = i.TecDocModel.SourceEntityModelHash })
                                                        .Select(g => g.First())
                                                        .ToList();
            return matchMakeModels;
        }

        //public async Task ImportModelMatch(string path) // TODO Move this to CSV library
        //{
        //    List<ImportMatchMakeModel> matchMakeModels = new();

        //    using (var reader = new StreamReader(path, Encoding.UTF8))
        //    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        //    {
        //        csv.Context.RegisterClassMap<ImportMatchMakeModelMap>();
        //        matchMakeModels = csv.GetRecords<ImportMatchMakeModel>().ToList();
        //    }

        //    await MappingService.CreateModelMatch(matchMakeModels.Where(c => !c.DeleteMatch).ToList());

        //    await MappingService.DeleteModelMatch(matchMakeModels.Where(c => c.DeleteMatch).ToList());
        //}

        public async Task UpdateCheckedStatus()
        {
            var builder = Builders<MatchMakeModel>.Filter;
            var updateFilter = builder.Eq(x => x.Status.Current.Status, Status.Check);

            await MatchMakeModelService.UpdateMatchStatus(Status.Checked, updateFilter);
        }

        #endregion

        #region Match Base

        public async Task<List<MatchBase>> GenerateMatchBase(MatchBaseType matchBaseType, string? path = null)
        {
            path ??= $"..\\MMIv8_Ktype\\Outputs\\{matchBaseType}Match.csv";

            // TODO Move this somewhere more appropriate
            using (var writer = new StreamWriter(path, false, Encoding.UTF8))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            using (var cursor = await MatchBaseService.GetAll(matchBaseType, batchSize: 10000))
            {
                while (await cursor.MoveNextAsync())
                {
                    csv.WriteRecords(cursor.Current.Select(m => m.BuildCsvObject()));
                }
            }

            return new(); // No return needed ATM but could use instead of csv
        }

        public async Task ImportMatchBase(string path) // TODO Move to CSV project
        {
            List<UpdateMatchBase> updatedMatches = new();
            using (var reader = new StreamReader(path))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                updatedMatches = csv.GetRecords<UpdateMatchBase>().ToList();
            }

            Log.Information("MatchBase import processing {Count} records", updatedMatches.Count);

            foreach (var matchBase in updatedMatches)
            {
                try
                {
                    await MappingService.UpdateMatchScore(matchBase.MatchBaseType, matchBase.MatchHash, matchBase.Score);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "MatchBase import failed {MatchBase}", matchBase.ToString());
                }
            }

            Log.Information("MatchBase import completed");
        }

        #endregion

        #region Match Entity

        public async Task BulkReloadAllEntityMatch()
        {
            await MatchEntityService.DeleteAll();


            var sw = Stopwatch.StartNew();

            List<Task> creates = new();
            int i = 0;
            using var makeModelMatches = await MatchMakeModelService.GetAllMatches(batchSize: 10);
            while (await makeModelMatches.MoveNextAsync())
            {
                var swInner = Stopwatch.StartNew();

                foreach (var makeModelMatch in makeModelMatches.Current)
                {
                    creates.Add(MappingService.StoreEntityMatch(makeModelMatch));
                }

                swInner.Stop();
                Log.Information("Processed {count} {time} {TotalTime}", i, swInner, sw);
                i++;
            }

            Task.WaitAll(creates.ToArray());

            sw.Stop();
            Log.Information("Complete {count} {TotalTime}", i, sw);

            await MappingService.RecalculateMatchBase();
        }

        public async Task<List<MatchEntity>> GenerateEntityMatch()
        {
            var sw = Stopwatch.StartNew();

            var filter = Builders<MatchEntity>.Filter.Eq(x => x.MatchResult.Failed, false);

            //// TODO Move this somewhere more appropriate
            using (var writer = new StreamWriter("..\\MMIv8_Ktype\\Outputs\\EntityMatch.csv", false, Encoding.UTF8))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            using (var cursor = await MatchEntityService.GetAll(filter, 1000))
            {
                csv.Context.RegisterClassMap<MatchEntityMap>();
                while (await cursor.MoveNextAsync())
                {
                    csv.WriteRecords(cursor.Current);
                }
            }

            sw.Stop();
            Log.Debug("Time: {0}", sw.Elapsed);
            return new(); // No return needed ATM but could use instead of csv
        }

        public async Task<List<MatchEntity>> GenerateEntityMatchRefine()
        {
            var sw = Stopwatch.StartNew();

            var filterBuilder = Builders<MatchEntity>.Filter;
            var filter = filterBuilder.Eq(c => c.MatchResult.Failed, false)
                       & (filterBuilder.Eq(c => c.MatchRefine.IsCheck, true) | filterBuilder.Size(c => c.MatchRefine.ChosenMatches, 0));

            //// TODO Move this somewhere more appropriate
            using (var writer = new StreamWriter("..\\MMIv8_Ktype\\Outputs\\EntityMatchRefine.csv", false, Encoding.UTF8))                
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            using (var cursor = await MatchEntityService.GetAll(filter, 1000))
            {
                csv.Context.RegisterClassMap<MatchEntityMap>();
                while (await cursor.MoveNextAsync())
                {
                    csv.WriteRecords(cursor.Current);
                }
            }

            sw.Stop();
            Log.Debug("Time: {0}", sw.Elapsed);
            return new(); // No return needed ATM but could use instead of csv
        }

        public async Task BulkUpdatePreviousMatchedFlag(string path)
        {
            List<UpdateFlagRequest> updateMatches = new();
            using (var reader = new StreamReader(path, Encoding.UTF8))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                updateMatches = csv.GetRecords<UpdateFlagRequest>().ToList();
            }

            Log.Information("Previous Match import processing {Count} records", updateMatches.Count);

            await MappingService.UpdatePreviousMatchedFlag(updateMatches);

            Log.Information("Previous Match import completed");
        }

        public async Task BulkUpdateMatchedFlag(string path)
        {
            List<UpdateFlagRequest> updateMatches = new();
            using (var reader = new StreamReader(path, Encoding.UTF8))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                updateMatches = csv.GetRecords<UpdateFlagRequest>().ToList();
            }

            Log.Information("Match Flag import processing {Count} records", updateMatches.Count());

            await MappingService.UpdateMatchedFlag(updateMatches);

            Log.Information("Match Flag import completed");
        }

        public async Task BulkUpdateFailedFlag(string path)
        {
            List<UpdateFlagRequest> updateMatches = new();
            using (var reader = new StreamReader(path, Encoding.UTF8))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                updateMatches = csv.GetRecords<UpdateFlagRequest>().ToList();
            }

            Log.Information("Failed Flag import processing {Count} records", updateMatches.Count);

            await MappingService.UpdateFailedFlag(updateMatches);

            Log.Information("Failed Flag import completed");
        }

        #endregion

        #region Entity Loading

        public async Task ReloadTecDocPCEntities(string path)
        {
            await SourceTecDocPCService.DeleteAll();

            List<MongoSourceTecDocPC> entities = [];
            using (var reader = new StreamReader(path, Encoding.UTF8))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Context.RegisterClassMap<MongoSourceTecDocPCMap>();
                entities = csv.GetRecords<MongoSourceTecDocPC>().ToList();
            }

            await SourceTecDocPCService.CreateBulk(entities);
        }

        public async Task UpdateTecDocPCEntities(string path)
        {
            List<MongoSourceTecDocPC> entities = [];
            using (var reader = new StreamReader(path, Encoding.UTF8))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Context.RegisterClassMap<MongoSourceTecDocPCMap>();
                entities = csv.GetRecords<MongoSourceTecDocPC>().ToList();
            }

            var filterBuilder = Builders<MongoSourceTecDocPC>.Filter;
            var filter = filterBuilder.Empty;

            filter = filterBuilder.Nin(c => c.KTypNr, entities.Select(c => c.KTypNr));
            var result = await SourceTecDocPCService.DeleteAll(filter);

            if (result.IsAcknowledged)
                Log.Information("Deleted {DeletedCount} vehicles no longer present", result.DeletedCount);

            foreach (var entity in entities)
            {
                await MappingService.UpdateEntity(entity);
            }
        }

        public async Task ReloadMMIv8Entities(string path)
        {
            await SourceMMIv8Service.DeleteAll();

            List<MongoSourceMMIv8> entities = [];
            using (var reader = new StreamReader(path, Encoding.UTF8))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Context.RegisterClassMap<MongoSourceMMIv8Map>();
                entities = csv.GetRecords<MongoSourceMMIv8>().ToList();
            }

            await SourceMMIv8Service.CreateBulk(entities);
        }

        public async Task UpdateMMIv8Entities(string path)
        {
            List<MongoSourceMMIv8> entities = [];
            using (var reader = new StreamReader(path, Encoding.UTF8))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Context.RegisterClassMap<MongoSourceMMIv8Map>();
                entities = csv.GetRecords<MongoSourceMMIv8>().ToList();
            }

            var filterBuilder = Builders<MongoSourceMMIv8>.Filter;
            var filter = filterBuilder.Empty;

            filter = filterBuilder.Nin(c => c.MMI_V8_Key, entities.Select(c => c.MMI_V8_Key));
            var result = await SourceMMIv8Service.DeleteAll(filter);

            if (result.IsAcknowledged)
                Log.Information("Deleted {DeletedCount} vehicles no longer present", result.DeletedCount);

            foreach (var entity in entities)
            {
                await MappingService.UpdateEntity(entity);
            }
        }

        #endregion
    }
}
