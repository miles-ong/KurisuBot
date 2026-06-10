using KurisuAPI.Records;
using KurisuAPI.Services;
using KurisuCore.Enums;
using KurisuCore.Models;
using KurisuCore.Result;
using Microsoft.AspNetCore.Mvc;

namespace KurisuAPI.Controllers
{
    [ApiController]
    [Route("anime")]
    public sealed class KAnimeController(KAnimeService animeService) : ControllerBase
    {
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            KAnime? anime = await animeService.GetByIdAsync(id);

            if (anime is null) return NotFound(KResult.CannotFindAnime);

            return Ok(anime);
        }

        [HttpGet("{id}/characters")]
        public async Task<IActionResult> GetCharacters(int id)
        {
            KAnime? anime = await animeService.GetByIdAsync(id);

            if (anime is null) return NotFound(KResult.CannotFindAnime);

            return Ok(anime!.Characters);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] int limit = 10) 
            => Ok(await animeService.SearchAsync(query, limit));

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] KAnimeRow req)
        {
            Result<KAnime> result = await animeService.AddAsync(req.Title, req.Description);

            return result.Success ? Ok(result.Value) : Conflict(result.Error);
        }

        [HttpPatch("{id}/title")]
        public async Task<IActionResult> SetTitle(int id, [FromBody] string newTitle)
        {
            KResult result = await animeService.SetTitleAsync(id, newTitle);

            return result == KResult.Completed ? Ok() : BadRequest(result);
        }

        [HttpPatch("{id}/description")]
        public async Task<IActionResult> SetDescription(int id, [FromBody] string newDescription)
        {
            KResult result = await animeService.SetDescriptionAsync(id, newDescription);

            return result == KResult.Completed ? Ok() : BadRequest(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            KResult result = await animeService.DeleteAsync(id);

            return result == KResult.Completed ? Ok() : NotFound(result);
        }
    }
}
