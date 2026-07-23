using KurisuAPI.Services;
using KurisuCore.Models.Data;
using KurisuCore.Result;
using Microsoft.AspNetCore.Mvc;

namespace KurisuAPI.Controllers
{
    [ApiController]
    [Route("characters")]
    public sealed class KCharacterController(KCharacterService characterService) : ControllerBase
    {
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var character = await characterService.GetByIdAsync(id);

            if (character is null) return NotFound(KDetail.CharacterNotFound);

            return Ok(character);
        }

        [HttpGet("{id:int}/anime")]
        public async Task<IActionResult> GetAnime(int id)
        {
            var character = await characterService.GetByIdAsync(id);

            if (character is null) return NotFound(KDetail.CharacterNotFound);

            return Ok(character.Anime);
        }

        [HttpGet("{id:int}/variants")]
        public async Task<IActionResult> GetVariants(int id)
        {
            var character = await characterService.GetByIdAsync(id);

            if (character is null) return NotFound(KDetail.CharacterNotFound);

            return Ok(character.Variants);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] int limit = 10) 
            => Ok(await characterService.SearchAsync(query, limit));

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] KCharacterData data)
        {
            var result = await characterService.AddAsync(data);

            return result.Success ? Ok(result.Value) : Conflict(result.Detail);
        }

        [HttpPatch("{id:int}/name")]
        public async Task<IActionResult> SetName(int id, [FromBody] string newName)
        {
            var detail = await characterService.SetNameAsync(id, newName);

            return detail == KDetail.Completed ? Ok() : BadRequest(detail);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var detail = await characterService.DeleteAsync(id);

            return detail == KDetail.Completed ? Ok() : NotFound(detail);
        }
    }
}
