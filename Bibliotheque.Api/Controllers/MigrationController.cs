using Bibliotheque.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bibliotheque.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MigrationController : ControllerBase
    {
        private readonly DataMigrationService _migrationService;

        public MigrationController(DataMigrationService migrationService)
        {
            _migrationService = migrationService;
        }

        [HttpPost("run")]
        public async Task<IActionResult> Run()
        {
            var result = await _migrationService.MigrateAsync();
            return Ok(new { message = result });
        }
    }
}