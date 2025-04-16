using Microsoft.AspNetCore.Mvc;
using MMIv8_Ktype.Core.Services;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MongoDB.Driver;

namespace MMIv8_Ktype.Core.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VersionController(VersionService VersionService, IVersionProvider versionProvider) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await VersionService.GetAll();
            return Ok(users.ToList());
        }

        [HttpGet("CurrentVersion")]
        public async Task<IActionResult> GetCurrentVersion()
        {
            var users = await VersionService.GetCurrentVersion();
            return Ok(users);
        }

        [HttpGet("{version}")]
        public async Task<IActionResult> GetByVersion(int version)
        {
            var user = await VersionService.GetByVersion(version);
            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateBaseVersion model)
        {
            await VersionService.Create(model);
            return Ok(new { message = "Version created" });
        }

        //[HttpPut]
        //public async Task<IActionResult> Replace(BaseVersion model)
        //{
        //    await VersionService.Replace(model);
        //    return Ok(new { message = "Version updated" });
        //}

        //[HttpDelete("{version}")]
        //public async Task<IActionResult> Delete(int version)
        //{
        //    await VersionService.Delete(version);
        //    return Ok(new { message = "Version deleted" });
        //}
    }
}
