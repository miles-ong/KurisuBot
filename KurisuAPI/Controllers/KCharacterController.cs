using KurisuAPI.Services;
using KurisuCore.Enums;
using KurisuCore.Models;
using KurisuCore.Result;
using Microsoft.AspNetCore.Mvc;

namespace KurisuAPI.Controllers
{
    [ApiController]
    [Route("characters")]
    public sealed class KCharacterController(KCharacterService characterService) : ControllerBase
    {
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            if (!characterService.TryGet(id, out KCharacter? character)) return NotFound(KResult.CannotFindCharacter);

            return Ok(character);
        }

        [HttpGet("search")]
        public IActionResult Search([FromQuery] string query, [FromQuery] int limit = 10) => Ok(characterService.Search(query, limit));

        [HttpPost]
        public async Task<IActionResult> AddAsync([FromBody] string name)
        {
            Result<KCharacter> result = await characterService.AddAsync(name);

            return result.Success ? Ok(result.Value) : Conflict(result.Error);
        }

        [HttpPatch("{id}/name")]
        public async Task<IActionResult> SetNameAsync(int id, [FromBody] string newName)
        {
            KResult result = await characterService.SetNameAsync(id, newName);

            return result == KResult.Completed ? Ok() : BadRequest(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            KResult result = await characterService.DeleteAsync(id);

            return result == KResult.Completed ? Ok() : NotFound(result);
        }
    }
}
