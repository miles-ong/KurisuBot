using KurisuAPI.Services;
using KurisuCore.Models.Data;
using KurisuCore.Result;
using Microsoft.AspNetCore.Mvc;

namespace KurisuAPI.Controllers
{
    [ApiController]
    [Route("anime")]
    public sealed class KAnimeController(KAnimeService animeService) : ControllerBase
    {
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var anime = await animeService.GetByIdAsync(id);

            if (anime is null) return NotFound(KDetail.AnimeNotFound);

            return Ok(anime);
        }

        [HttpGet("{id:int}/characters")]
        public async Task<IActionResult> GetCharacters(int id)
        {
            var anime = await animeService.GetByIdAsync(id);

            if (anime is null) return NotFound(KDetail.AnimeNotFound);

            return Ok(anime.Characters);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] int limit = 10) 
            => Ok(await animeService.SearchAsync(query, limit));

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] KAnimeData data)
        {
            var result = await animeService.AddAsync(data);

            return result.Success ? Ok(result.Value) : Conflict(result.Detail);
        }

        [HttpPatch("{id:int}/title")]
        public async Task<IActionResult> SetTitle(int id, [FromBody] string newTitle)
        {
            var detail = await animeService.SetTitleAsync(id, newTitle);

            return detail == KDetail.Completed ? Ok() : BadRequest(detail);
        }

        [HttpPatch("{id:int}/description")]
        public async Task<IActionResult> SetDescription(int id, [FromBody] string newDescription)
        {
            var detail = await animeService.SetDescriptionAsync(id, newDescription);

            return detail == KDetail.Completed ? Ok() : BadRequest(detail);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var detail = await animeService.DeleteAsync(id);

            return detail == KDetail.Completed ? Ok() : NotFound(detail);
        }
    }
}
