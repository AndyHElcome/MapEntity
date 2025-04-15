using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Source;
using MMIv8_Ktype.Models.Collections;
using MongoDB.Bson;
using System.Globalization;
using System.Text;

namespace MMIv8_Ktype.Core.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SourceMMIv8Controller(ISourceEntityService<MongoSourceMMIv8> SourceMMIv8Service, 
                                       BulkMappingService BulkMappingService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var entities = await SourceMMIv8Service.GetAll();
            return Ok(entities);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(ObjectId id)
        {
            var entities = await SourceMMIv8Service.GetById(id);
            return Ok(entities);
        }

        [HttpPost("Update/{Path}")]
        public async Task<IActionResult> UpdateEntities(string Path = "C:\\Users\\andy.hargreaves\\OneDrive - Elcome Ltd\\Desktop\\NEW MMI TO KTYPE\\Source_MMIv8.txt")
        {
            await BulkMappingService.UpdateMMIv8Entities(Path);
            return Ok(new { message = "Index Loaded" });
        }

        [HttpPost("ReloadFromCSV/{Path}")]
        public async Task<IActionResult> LoadAll(string Path = "C:\\Users\\andy.hargreaves\\OneDrive - Elcome Ltd\\Desktop\\NEW MMI TO KTYPE\\Source_MMIv8.txt")
        {
            await BulkMappingService.ReloadMMIv8Entities(Path);
            return Ok(new { message = "Index Loaded" });
        }

        [HttpPost("GenerateCSV")]
        public async Task<IActionResult> GenerateCSV()
        {
            using var writer = new StreamWriter("..\\MMIv8_Ktype\\Outputs\\MMIv8.csv", false, Encoding.UTF8);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            using var entities = await SourceMMIv8Service.GetAll(batchSize: 1000);
            while (await entities.MoveNextAsync())
            {
                csv.Context.RegisterClassMap<MongoSourceMMIv8Map>();
                csv.WriteRecords(entities.Current);
            }
            return Ok(new { message = "Created" });
        }
    }
}
