using Microsoft.AspNetCore.Http.HttpResults;
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
using System.Reflection.Metadata;
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
        public async Task<Result<List<MatchMakeModel>>> GenerateMakeModelMatch()//TODO Use Linq query builder could be endpoint
        {
            try
            {
                List<MatchMakeModel> currentMatch = await MatchMakeModelService.GetFindFluent().ToListAsync();

                var MMIv8Models = await SourceMMIv8EntityModelService.GetByNotId(currentMatch.Select(m => m.MMIv8Model.DocumentId).ToArray());
                if (!MMIv8Models.IsSuccess && MMIv8Models.Error!.Type != ErrorType.NoContent)
                    return MMIv8Models.Error;
                var MMINoMatches = MMIv8Models.Value.ToList().Select(m => new MatchMakeModel(tecDocModel: new(), mmiv8Model: m, versionProvider)).ToList();

                var TecDocPCModels = await SourceTecDocEntityModelService.GetByNotId(currentMatch.Select(m => m.TecDocModel.DocumentId).ToArray());
                if (!TecDocPCModels.IsSuccess && TecDocPCModels.Error!.Type != ErrorType.NoContent)
                    return TecDocPCModels.Error;
                var TecDocNoMatches = TecDocPCModels.Value.ToList().Select(m => new MatchMakeModel(tecDocModel: m, mmiv8Model: new(), versionProvider)).ToList();

                List<MatchMakeModel> matchMakeModels = currentMatch
                                                            .Union(MMINoMatches)
                                                            .Union(TecDocNoMatches)
                                                            .GroupBy(i => new { mmiHash = i.MMIv8Model.DocumentId, tdHash = i.TecDocModel.DocumentId })
                                                            .Select(g => g.First())
                                                            .OrderBy(o => string.Concat(o.MMIv8Model.Make, o.TecDocModel.Make))
                                                            .ThenBy(o => string.Concat(o.MMIv8Model.Model, o.TecDocModel.Model))
                                                            .ToList();

                return matchMakeModels is { Count: > 0 } ? matchMakeModels : Error.NoContent("MatchMakeModel.NoContent", "No content found when trying to GenerateMakeModelMatch");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting Documents for GenerateMakeModelMatch");
                return Error.Failure($"MatchMakeModel.GenerateMakeModelMatchFailure", $"Error getting Documents. Error: {ex.Message}");
            }
        }

        public async Task<Result> PopulateMakeModelMatch()
        {
            try
            {
                List<MatchMakeModel> currentMatch = await MatchMakeModelService.GetFindFluent().ToListAsync();
                List<MatchMakeModel> newMatchMakeModels = new();

                var MMIv8Models = await SourceMMIv8EntityModelService.GetByNotId(currentMatch.Select(m => m.MMIv8Model.DocumentId).ToArray());
                if (!MMIv8Models.IsSuccess && MMIv8Models.Error!.Type != ErrorType.NoContent)
                    return MMIv8Models.Error;

                var AllTecDocPCModels = await SourceTecDocEntityModelService.GetFindFluent().ToListAsync();

                foreach (var newMMIv8Model in MMIv8Models.Value)
                {
                    var quickMatches = AllTecDocPCModels.Where(t => t.Make.RemoveSpecialCharacters() == newMMIv8Model.Make.RemoveSpecialCharacters() && t.Model.RemoveSpecialCharacters() == newMMIv8Model.Model.RemoveSpecialCharacters()).ToList();
                    
                    if (quickMatches.Count != 0)
                        newMatchMakeModels.AddRange(quickMatches.Select(td => new MatchMakeModel(tecDocModel: td, mmiv8Model: newMMIv8Model, versionProvider)));
                    else
                        newMatchMakeModels.Add(new MatchMakeModel(tecDocModel: new(), mmiv8Model: newMMIv8Model, versionProvider));
                }

                var TecDocPCModels = await SourceTecDocEntityModelService.GetByNotId(currentMatch.Select(m => m.TecDocModel.DocumentId).ToArray());
                if (!TecDocPCModels.IsSuccess && TecDocPCModels.Error!.Type != ErrorType.NoContent)
                    return TecDocPCModels.Error;


                var AllMMIv8Models = await SourceMMIv8EntityModelService.GetFindFluent().ToListAsync();

                foreach (var newTecDocPCModel in TecDocPCModels.Value)
                {
                    var quickMatches = AllMMIv8Models.Where(t => t.Make.RemoveSpecialCharacters() == newTecDocPCModel.Make.RemoveSpecialCharacters() && t.Model.RemoveSpecialCharacters() == newTecDocPCModel.Model.RemoveSpecialCharacters()).ToList();

                    if (quickMatches.Count != 0)
                        newMatchMakeModels.AddRange(quickMatches.Select(mmi => new MatchMakeModel(tecDocModel: newTecDocPCModel, mmiv8Model: mmi, versionProvider)));
                    else
                        newMatchMakeModels.Add(new MatchMakeModel(tecDocModel: newTecDocPCModel, mmiv8Model: new(), versionProvider));
                }

                foreach(var newMatchMakeModel in newMatchMakeModels.GroupBy(i => new { mmiHash = i.MMIv8Model.DocumentId, tdHash = i.TecDocModel.DocumentId }).Select(g => g.Key))
                    await MappingService.CreateMakeModelMatch(newMatchMakeModel.tdHash, newMatchMakeModel.mmiHash);

                return Result.Success();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting Documents for GenerateMakeModelMatch");
                return Error.Failure($"MatchMakeModel.GenerateMakeModelMatchFailure", $"Error getting Documents. Error: {ex.Message}");
            }
        }
        #endregion

        #region Match Entity

        public async Task BulkReloadAllEntityMatch()
        {
            await MatchEntityService.DeleteAll();
            var sw = Stopwatch.StartNew();

            List<Task> creates = new();
            int i = 0;
            using var makeModelMatches = await MatchMakeModelService.GetAllMatches(batchSize: 100);
            while (await makeModelMatches.MoveNextAsync())
            {
                var swInner = Stopwatch.StartNew();

                foreach (var makeModelMatch in makeModelMatches.Current)
                {
                    creates.Add(MappingService.StoreEntityMatch(makeModelMatch));
                    //await MappingService.StoreEntityMatch(makeModelMatch);
                }

                swInner.Stop();
                Log.Information("Processed {count} {time} {TotalTime}", i, swInner, sw);
                i++;
            }

            Task.WaitAll(creates.ToArray());

            sw.Stop();
            Log.Information("Complete {count} {TotalTime}", i, sw);
        }

        #endregion
    }
}
