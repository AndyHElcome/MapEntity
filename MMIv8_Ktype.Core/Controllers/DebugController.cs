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
            await MatchEntityService.DeleteByFilter(filter);

            var matchMakeModel= await MatchMakeModelService.GetById(ObjectId.Parse(objectId));

            if (matchMakeModel.IsSuccess)
                await MappingService.StoreEntityMatch(matchMakeModel.Value);

            return Ok("updated");
        }

        [HttpPost("Entity/BulkRecalculateMatchBase")]
        public async Task<IActionResult> BulkRecalculateMatchBase()
        {
            await MappingService.RecalculateMatchBase();
            return Ok("updated");
        }

        //[HttpPost("Entity/BulkRevalidateFailures")]
        //public async Task<IActionResult> BulkRevalidateFailures()
        //{
        //    await MatchEntityService.RevalidateFailures();
        //    return Ok("updated");
        //}

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

        //[HttpPost("MakeModel/UpdateCheckedStatus")]
        //public async Task<IActionResult> UpdateCheckedStatus()
        //{
        //    await BulkMappingService.UpdateCheckedStatus();
        //    return Ok("updated");
        //}

        [HttpPost("Indexes/RegenerateIndexes")]
        public IActionResult RegenerateIndexes()
        {
            mongoDBContext.Collections.CreateAllIndexes(true);
            return Ok("updated");
        }
    }
}
