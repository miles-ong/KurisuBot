using KurisuAPI.Services;
using KurisuCore.Models;
using KurisuCore.Result;
using Microsoft.AspNetCore.Mvc;

namespace KurisuAPI.Controllers
{
    [ApiController]
    [Route("calinks")]
    public sealed class KCharacterAnimeLinkController(
        KCharacterAnimeLinkService linkService,
        KCharacterService characterService,
        KAnimeService animeService) 
        : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> LinkAsync([FromBody] LinkRequest req)
        {
            var detail = await linkService.LinkAsync(req.CharacterId, req.AnimeId);

            return detail == KDetail.Completed ? Ok() : BadRequest(detail);
        }

        [HttpDelete]
        public async Task<IActionResult> UnlinkAsync([FromBody] KCharacter character, [FromBody] KAnime anime)
        {
            var detail = await linkService.UnlinkAsync(character, anime);

            return detail == KDetail.Completed ? Ok() : BadRequest(detail);
        }

        public record LinkRequest(int CharacterId, int AnimeId);
    }
}
