using KurisuAPI.Services;
using KurisuCore.Enums;
using Microsoft.AspNetCore.Mvc;

namespace KurisuAPI.Controllers
{
    [ApiController]
    [Route("calinks")]
    public sealed class KCharacterAnimeLinkController(KCharacterAnimeLinkService linkService) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> LinkAsync([FromBody] LinkRequest req)
        {
            KResult result = await linkService.LinkAsync(req.CharacterId, req.AnimeId);

            return result == KResult.Completed ? Ok() : BadRequest(result);
        }

        [HttpDelete]
        public async Task<IActionResult> UnlinkAsync([FromBody] LinkRequest req)
        {
            KResult result = await linkService.UnlinkAsync(req.CharacterId, req.AnimeId);

            return result == KResult.Completed ? Ok() : BadRequest(result);
        }

        public record LinkRequest(int CharacterId, int AnimeId);
    }
}
