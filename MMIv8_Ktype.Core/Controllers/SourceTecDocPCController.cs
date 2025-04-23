using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Source;
using MMIv8_Ktype.Models.Collections;
using MongoDB.Bson;

namespace MMIv8_Ktype.Core.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SourceTecDocPCController(ISourceEntityService<MongoSourceTecDocPC> SourceTecDocPCService,
                                          BulkMappingService BulkMappingService,
                                          MappingService MappingService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await SourceTecDocPCService.GetAll();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(ObjectId id)
        {
            var result = await SourceTecDocPCService.GetById(id);
            return Ok(result);
        }

        //[HttpPost]
        //public async Task<IActionResult> Create(MongoSourceTecDocPC model)
        //{
        //    await _service.Create(model);
        //    return Ok(new { message = "User created" });
        //}

        [HttpPost("UpdateEntity")]
        public async Task<IActionResult> UpdateEntity(MongoSourceTecDocPC Entity)
        {
            await MappingService.UpdateEntity(Entity);
            return Ok(new { message = "Entity Updated" });
        }

        [HttpPost("UpdateEntities/{Path}")]
        public async Task<IActionResult> UpdateEntities(string Path = "C:\\Users\\andy.hargreaves\\OneDrive - Elcome Ltd\\Desktop\\NEW MMI TO KTYPE\\Source_TD_PC.txt")
        {
            await BulkMappingService.UpdateTecDocPCEntities(Path);
            return Ok(new { message = "Entities Updated" });
        }

        [HttpPost("ReloadFromCSV/{Path}")]
        public async Task<IActionResult> ReloadFromCSV(string Path = "C:\\Users\\andy.hargreaves\\OneDrive - Elcome Ltd\\Desktop\\NEW MMI TO KTYPE\\Source_TD_PC.txt")
        {
            await BulkMappingService.ReloadTecDocPCEntities(Path);
            return Ok(new { message = "Index Reloaded" });
        }

        // [HttpPost("GenerateCSV")]
        // public async Task<IActionResult> GenerateCSV()
        // {
        //     using var writer = new StreamWriter("..\\MMIv8_Ktype\\Outputs\\TecDocPC.csv", false, Encoding.UTF8);
        //     using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        //     using var entities = await SourceTecDocPCService.GetAll();
        //     while (await entities.MoveNextAsync())
        //     {
        //         csv.Context.RegisterClassMap<MongoSourceMMIv8Map>();
        //         csv.WriteRecords(entities.Current);
        //     }
        //     return Ok(new { message = "Created" });
        // }
    }
}
