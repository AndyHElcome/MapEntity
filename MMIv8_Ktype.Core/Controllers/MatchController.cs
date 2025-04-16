using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Util;

namespace MMIv8_Ktype.Core.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MatchController(MatchEntityService MatchEntityService,
                                 MappingService MappingService,
                                 BulkMappingService BulkMappingService) : ControllerBase
    {
        [HttpPost("MatchEntity/GenerateEntityMatchCSV")] //TODO change to stream call
        public async Task<IActionResult> GenerateEntityMatchCSV()
        {
            return Ok(await BulkMappingService.GenerateEntityMatch());
        }

        [HttpPost("MatchEntity/GenerateEntityMatchRefineCSV")] //TODO change to stream call
        public async Task<IActionResult> GenerateEntityMatchRefineCSV()
        {
            return Ok(await BulkMappingService.GenerateEntityMatchRefine());
        }

        [HttpPost("MatchEntity/UpdateMatchedFlag")]
        public async Task<IActionResult> UpdateMatchedFlag(int KTypNr, int MMI_V8_Key, bool Flag, string? Detail = null)
        {
            await MappingService.UpdateMatchedFlag([new UpdateEntityMatchFlag(KTypNr, MMI_V8_Key, Flag, Detail) ]);
            return Ok("updated");
        }

        [HttpPost("MatchEntity/BulkUpdateMatchedFlag")]
        public async Task<IActionResult> BulkUpdateMatchedFlag(string Path)
        {
            await BulkMappingService.BulkUpdateMatchedFlag(Path);
            return Ok("updated");
        }

        [HttpPost("MatchEntity/UpdateFailedFlag")]
        public async Task<IActionResult> UpdateFailedFlag(int KTypNr, int MMI_V8_Key, bool Flag, string? Detail = null)
        {
            await MappingService.UpdateFailedFlag([new UpdateEntityMatchFlag(KTypNr, MMI_V8_Key, Flag, Detail) ]);
            return Ok("updated");
        }

        [HttpPost("MatchEntity/BulkUpdateFailedFlag")]
        public async Task<IActionResult> BulkUpdateFailedFlag(string Path)
        {
            await BulkMappingService.BulkUpdateFailedFlag(Path);
            return Ok("updated");
        }

        [HttpPost("MatchEntity/UpdateMatchRefineStatus")]
        public async Task<IActionResult> UpdateMatchRefineStatus(int[] MMI_V8_Keys)
        {
            await MappingService.UpdateMatchRefineStatus(MMI_V8_Keys);
            return Ok("updated");
        }
        
        [HttpPost("MatchMakeModel/GenerateModelMatchCSV")]
        public async Task<IActionResult> GenerateModelMatchCSV()
        {
            return Ok(await BulkMappingService.GenerateModelMatch());
        }

        [HttpPost("MatchMakeModel/ImportModelMatchCSV")]
        public async Task<IActionResult> ImportModelMatchCSV(string Path = "D:\\GIT\\MMIv8_Ktype\\Outputs\\modelMatchLoad.csv")
        {
            await BulkMappingService.ImportModelMatch(Path);
            return Ok("created");
        }

        [HttpPost("Base/StorePartialMatchBase")]
        public async Task<IActionResult> StorePartialMatchBase(MatchBaseType MatchBaseType, string MatchHash, decimal? NewScore = null)
        {
            await MappingService.StorePartialMatchBase(MatchBaseType, MatchHash, NewScore);
            return Ok("updated");
        }

        [HttpPost("Base/RemovePartialMatchBase")]
        public async Task<IActionResult> RemovePartialMatchBase(MatchBaseType MatchBaseType, string MatchHash)
        {
            await MappingService.RemovePartialMatchBase(MatchBaseType, MatchHash);
            return Ok("removed");
        }

        [HttpPost("Base/UpdateMatchBaseScore")]
        public async Task<IActionResult> UpdateMatchBaseScore(MatchBaseType MatchBaseType, string MatchHash, decimal NewScore)
        {
            await MappingService.UpdateMatchScore(MatchBaseType, MatchHash, NewScore);
            return Ok("updated");
        }

        [HttpPost("Base/GenerateMatchBaseCSV")]
        public async Task<IActionResult> GenerateMatchBase(MatchBaseType matchBaseType, string? Path = null)
        {
            await BulkMappingService.GenerateMatchBase(matchBaseType, Path);
            return Ok("created");
        }

        [HttpPost("Base/ImportMatchBaseCSV")] //TODO change to stream call
        public async Task<IActionResult> ImportMatchBaseCSV(string Path = "C:\\Users\\andy.hargreaves\\OneDrive - Elcome Ltd\\Desktop\\NEW MMI TO KTYPE\\IMPORT_Body.csv")
        {
            await BulkMappingService.ImportMatchBase(Path);
            return Ok("updated");
        }

        [HttpPost("Check/EntityMatch")]
        public async Task<IActionResult> EntityMatch(int KtypNr, int MMI_V8_Key)
        {
            return Ok(await MappingService.CheckMatchVadlidity(KtypNr, MMI_V8_Key));
        }
    }
}
