using MMIv8_Ktype.Api.Requests;
using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Core.Services.Source;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Indexes;
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
                                                        .OrderBy(o => String.Concat(o.MMIv8Model.Make, o.TecDocModel.Make))
                                                        .ThenBy(o  => String.Concat(o.MMIv8Model.Model, o.TecDocModel.Model))
                                                        .ToList();
            return matchMakeModels;
        }

        public async Task UpdateCheckedStatus()
        {
            var builder = Builders<MatchMakeModel>.Filter;
            var updateFilter = builder.Eq(x => x.Status.Current.Status, Status.Check);

            await MatchMakeModelService.UpdateMatchStatus(Status.Checked, updateFilter);
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

        #endregion

        #region Entity Loading

        //public async Task ReloadTecDocPCEntities(string path)
        //{
        //    await SourceTecDocPCService.DeleteAll();

        //    List<MongoSourceTecDocPC> entities = [];
        //    // using (var reader = new StreamReader(path, Encoding.UTF8))
        //    // using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        //    // {
        //    //     csv.Context.RegisterClassMap<MongoSourceTecDocPCMap>();
        //    //     entities = csv.GetRecords<MongoSourceTecDocPC>().ToList();
        //    // }

        //    await SourceTecDocPCService.CreateBulk(entities);
        //}

        //public async Task UpdateTecDocPCEntities(string path)
        //{
        //    List<MongoSourceTecDocPC> entities = [];
        //    // using (var reader = new StreamReader(path, Encoding.UTF8))
        //    // using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        //    // {
        //    //     csv.Context.RegisterClassMap<MongoSourceTecDocPCMap>();
        //    //     entities = csv.GetRecords<MongoSourceTecDocPC>().ToList();
        //    // }

        //    var filterBuilder = Builders<MongoSourceTecDocPC>.Filter;
        //    var filter = filterBuilder.Empty;

        //    filter = filterBuilder.Nin(c => c.KTypNr, entities.Select(c => c.KTypNr));
        //    var result = await SourceTecDocPCService.DeleteAll(filter);

        //    if (result.IsAcknowledged)
        //        Log.Information("Deleted {DeletedCount} vehicles no longer present", result.DeletedCount);

        //    foreach (var entity in entities)
        //    {
        //        await MappingService.UpdateEntity(entity);
        //    }
        //}

        //public async Task ReloadMMIv8Entities(string path)
        //{
        //    await SourceMMIv8Service.DeleteAll();

        //    List<MongoSourceMMIv8> entities = [];
        //    // using (var reader = new StreamReader(path, Encoding.UTF8))
        //    // using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        //    // {
        //    //     csv.Context.RegisterClassMap<MongoSourceMMIv8Map>();
        //    //     entities = csv.GetRecords<MongoSourceMMIv8>().ToList();
        //    // }

        //    await SourceMMIv8Service.CreateBulk(entities);
        //}

        //public async Task UpdateMMIv8Entities(string path)
        //{
        //    List<MongoSourceMMIv8> entities = [];
        //    // using (var reader = new StreamReader(path, Encoding.UTF8))
        //    // using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        //    // {
        //    //     csv.Context.RegisterClassMap<MongoSourceMMIv8Map>();
        //    //     entities = csv.GetRecords<MongoSourceMMIv8>().ToList();
        //    // }

        //    var filterBuilder = Builders<MongoSourceMMIv8>.Filter;
        //    var filter = filterBuilder.Empty;

        //    filter = filterBuilder.Nin(c => c.MMI_V8_Key, entities.Select(c => c.MMI_V8_Key));
        //    var result = await SourceMMIv8Service.DeleteAll(filter);

        //    if (result.IsAcknowledged)
        //        Log.Information("Deleted {DeletedCount} vehicles no longer present", result.DeletedCount);

        //    foreach (var entity in entities)
        //    {
        //        await MappingService.UpdateEntity(entity);
        //    }
        //}

        //public async Task UpdateMMIv8Entity(SourceEntity sourceEntity)
        //{
        //    List<MongoSourceMMIv8> entities = [];
        //    // using (var reader = new StreamReader(path, Encoding.UTF8))
        //    // using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        //    // {
        //    //     csv.Context.RegisterClassMap<MongoSourceMMIv8Map>();
        //    //     entities = csv.GetRecords<MongoSourceMMIv8>().ToList();
        //    // }

        //    var filterBuilder = Builders<MongoSourceMMIv8>.Filter;
        //    var filter = filterBuilder.Empty;

        //    filter = filterBuilder.Nin(c => c.MMI_V8_Key, entities.Select(c => c.MMI_V8_Key));
        //    var result = await SourceMMIv8Service.DeleteAll(filter);

        //    if (result.IsAcknowledged)
        //        Log.Information("Deleted {DeletedCount} vehicles no longer present", result.DeletedCount);

        //    foreach (var entity in entities)
        //    {
        //        await MappingService.UpdateEntity(entity);
        //    }
        //}

        #endregion
    }
}
