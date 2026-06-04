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
        public IActionResult Get(int id)
        {
            if (!animeService.TryGet(id, out KAnime? anime)) return NotFound(KResult.CannotFindAnime);

            return Ok(anime);
        }

        [HttpGet("{id}/characters")]
        public IActionResult GetCharacters(int id)
        {
            if (!animeService.TryGet(id, out KAnime? anime)) return NotFound(KResult.CannotFindAnime);

            return Ok(anime!.Characters);
        }

        [HttpGet("search")]
        public IActionResult Search([FromQuery] string query, [FromQuery] int limit = 10) => Ok(animeService.Search(query, limit));

        [HttpPost]
        public async Task<IActionResult> AddAsync([FromBody] AddAnimeRequest req)
        {
            Result<KAnime> result = await animeService.AddAsync(req.Title, req.Description);

            return result.Success ? Ok(result.Value) : Conflict(result.Error);
        }

        [HttpPatch("{id}/title")]
        public async Task<IActionResult> SetTitleAsync(int id, [FromBody] string newTitle)
        {
            KResult result = await animeService.SetTitleAsync(id, newTitle);

            return result == KResult.Completed ? Ok() : BadRequest(result);
        }

        [HttpPatch("{id}/description")]
        public async Task<IActionResult> SetDescriptionAsync(int id, [FromBody] string newDescription)
        {
            KResult result = await animeService.SetDescriptionAsync(id, newDescription);

            return result == KResult.Completed ? Ok() : BadRequest(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            KResult result = await animeService.DeleteAsync(id);

            return result == KResult.Completed ? Ok() : NotFound(result);
        }

        public record AddAnimeRequest(string Title, string? Description);
    }
}
