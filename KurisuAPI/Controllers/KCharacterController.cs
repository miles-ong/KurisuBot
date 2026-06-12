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
        public async Task<IActionResult> Get(int id)
        {
            KCharacter? character = await characterService.GetByIdAsync(id);

            if (character is null) return NotFound(KResult.CannotFindCharacter);

            return Ok(character);
        }

        [HttpGet("{id}/animes")]
        public async Task<IActionResult> GetAnimes(int id)
        {
            KCharacter? character = await characterService.GetByIdAsync(id);

            if (character is null) return NotFound(KResult.CannotFindAnime);

            return Ok(character.Animes);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] int limit = 10) 
            => Ok(await characterService.SearchAsync(query, limit));

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] string name)
        {
            Result<KCharacter> result = await characterService.AddAsync(name);

            return result.Success ? Ok(result.Value) : Conflict(result.Error);
        }

        [HttpPatch("{id}/name")]
        public async Task<IActionResult> SetName(int id, [FromBody] string newName)
        {
            KResult result = await characterService.SetNameAsync(id, newName);

            return result == KResult.Completed ? Ok() : BadRequest(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            KResult result = await characterService.DeleteAsync(id);

            return result == KResult.Completed ? Ok() : NotFound(result);
        }
    }
}
