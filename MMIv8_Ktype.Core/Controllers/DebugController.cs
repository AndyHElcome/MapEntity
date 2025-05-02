using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Core.Services;
using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Status;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;
using System.Diagnostics;
using System.Linq;

namespace MMIv8_Ktype.Core.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DebugController(MatchEntityService MatchEntityService,
                                 MatchMakeModelService MatchMakeModelService,
                                 MappingService MappingService,
                                 BulkMappingService BulkMappingService,
                                 MongoDBContext mongoDBContext,
                                 IVersionProvider versionProvider) : ControllerBase
    {
        [HttpPost("Entity/BulkReloadAllEntityMatch")]
        public async Task<IActionResult> BulkReloadAllEntityMatch()
        {
            await BulkMappingService.BulkReloadAllEntityMatch();
            return Ok("updated");
        }

        [HttpPost("Entity/TestBulkReloadAllEntityMatch/{objectId}")]
        public async Task<IActionResult> BulkReloadAllEntityMatch(string objectId)
        {
            var filter = Builders<MatchEntity>.Filter.Eq(c => c.MatchMakeModelMatchID, ObjectId.Parse(objectId));
            await MatchEntityService.Delete(filter);

            var matchMakeModel= await MatchMakeModelService.GetById(ObjectId.Parse(objectId));

            await MappingService.StoreEntityMatch(matchMakeModel!);

            return Ok("updated");
        }

        [HttpPost("Entity/BulkRecalculateMatchBase")]
        public async Task<IActionResult> BulkRecalculateMatchBase()
        {
            await MappingService.RecalculateMatchBase();
            return Ok("updated");
        }

        [HttpPost("Entity/BulkRevalidateFailures")]
        public async Task<IActionResult> BulkRevalidateFailures()
        {
            await MatchEntityService.RevalidateFailures();
            return Ok("updated");
        }

        [HttpPost("Entity/BulkUpdateMatchRefine")]
        public async Task<IActionResult> BulkUpdateMatchRefine(int MMI_V8_Key)
        {
            var matchRefineUpdate = await MatchEntityService.BulkCombinationUpdateMatchRefine( [ MMI_V8_Key ] );
            var matchRefineResult = await matchRefineUpdate.CommitBulkWrite();
            return Ok(matchRefineResult);
        }

        [HttpPost("Entity/UpdateStatus/{Status}")]
        public async Task<IActionResult> UpdateStatus(Status Status)
        {
            var filter = Builders<MatchEntity>.Filter.Empty;
            var matchEntityUpdate = new CombinationPipeline<MatchEntity>(MatchEntityService.Collection, filter).AppendPipeline(c => c.AppendStatus(versionProvider.NewStatus(Status)));
            var matchEntityResult = await matchEntityUpdate.UpdateDocuments();
            return Ok("updated");
        }

        [HttpPost("Entity/RemoveStatus/{Status}")]
        public async Task<IActionResult> RemoveStatus(Status Status)
        {
            var filter = Builders<MatchEntity>.Filter.Empty;
            var matchEntityUpdate = new CombinationPipeline<MatchEntity>(MatchEntityService.Collection, filter).AppendPipeline(c => c.RemoveStatus(Status));
            var matchEntityResult = await matchEntityUpdate.UpdateDocuments();
            return Ok("updated");
        }

        [HttpPost("MakeModel/UpdateCheckedStatus")]
        public async Task<IActionResult> UpdateCheckedStatus()
        {
            await BulkMappingService.UpdateCheckedStatus();
            return Ok("updated");
        }

        [HttpPost("Indexes/RegenerateIndexes")]
        public IActionResult RegenerateIndexes()
        {
            mongoDBContext.Collections.CreateAllIndexes(true);
            return Ok("updated");
        }

        [HttpPost("Test/CheckPrev")]
        public async Task<IActionResult> CheckPrev(string objectId)
        {
            var filter = Builders<MatchEntity>.Filter.Eq(c => c.MatchMakeModelMatchID, ObjectId.Parse(objectId));
            var matches = await MatchEntityService.GetAll(filter);

            var matches2 = await matches.ToListAsync();

            var matches3 = matches2.Select(c => ( c.TecDocEntity.KTypNr, c.MMIv8Entity.MMI_V8_Key)).ToList();

            var sw = Stopwatch.StartNew();

            //Log.Information("test1 start");
            //sw.Restart();
            //var test1 = MappingService.CheckPreviousMatchedFlag_test1(matches3);
            //var result = new List<EntityRelation>();
            //await foreach(var t in test1)
            //{
            //    if (t is not null)
            //        result.Add(t);
            //}
            //sw.Stop();    
            //Log.Information("test1 stopped {count} {time}", result.Count, sw);
            //test1 = null;

            //Log.Information("test2 start");
            //sw.Restart();
            //var test2 = MappingService.CheckPreviousMatchedFlag_test2(matches3);
            //result = new List<EntityRelation>();
            //await foreach (var t in test2)
            //{
            //    if (t is not null)
            //        result.Add(t);
            //}
            //sw.Stop();
            //Log.Information("test2 stopped {count} {time}", result.Count, sw);
            //test2 = null;

            Log.Information("test3 start");
            sw.Restart();
            var test3 = await MappingService.CheckPreviousMatchedFlag(matches3);
            sw.Stop();
            Log.Information("test3 stopped {count} {time}", test3.Count, sw);



            return Ok("complete");
        }

        //[HttpPost("Indexes/UpdatePreviousMatch")]
        //public async Task<IActionResult> UpdatePreviousMatch(string Path)
        //{
        //    await BulkMappingService.BulkUpdatePreviousMatchedFlag(Path);
        //    return Ok("updated");
        //}
    }
}
