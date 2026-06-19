using KurisuAPI.Services;
using KurisuCore.Enums;
using KurisuCore.Models;
using KurisuCore.Result;
using Microsoft.AspNetCore.Mvc;

namespace KurisuAPI.Controllers
{
    [ApiController]
    [Route("variant")]
    public sealed class KVariantController(KVariantService variantService) : ControllerBase
    {
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            KVariant? variant = await variantService.GetByIdAsync(id);

            if (variant is null) return NotFound(KResult.CannotFindVariant);

            return Ok(variant);
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AddVariantRequest req)
        {
            Result<KVariant> result = await variantService.AddAsync(req.CharacterId, req.AnimeId, req.Rarity, req.Title);

            return result.Success ? Ok(result.Value) : Conflict(result.Error);
        }

        [HttpPatch("{id}/anime")]
        public async Task<IActionResult> SetAnime(int id, [FromBody] KAnime newAnime)
        {
            KResult result = await variantService.SetAnimeAsync(id, newAnime);

            return result == KResult.Completed ? Ok() : BadRequest(result);
        }

        [HttpPatch("{id}/rarity")]
        public async Task<IActionResult> SetRarity(int id, [FromBody] KRarity newRarity)
        {
            KResult result = await variantService.SetRarityAsync(id, newRarity);

            return result == KResult.Completed ? Ok() : BadRequest(result);
        }

        [HttpPatch("{id}/title")]
        public async Task<IActionResult> SetTitle(int id, [FromBody] string newTitle)
        {
            KResult result = await variantService.SetTitleAsync(id, newTitle);

            return result == KResult.Completed ? Ok() : BadRequest(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            KResult result = await variantService.DeleteAsync(id);

            return result == KResult.Completed ? Ok() : NotFound(result);
        }

        public record AddVariantRequest(int CharacterId, int AnimeId, KRarity Rarity, string Title);
    }
}
