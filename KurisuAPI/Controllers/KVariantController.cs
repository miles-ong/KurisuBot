using KurisuAPI.Services;
using KurisuCore.Enums;
using KurisuCore.Models;
using KurisuCore.Result;
using Microsoft.AspNetCore.Mvc;

namespace KurisuAPI.Controllers
{
    public sealed class KVariantController(KVariantService variantService) : ControllerBase
    {
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            if (!variantService.TryGet(id, out KVariant? variant)) return NotFound(KResult.CannotFindVariant);

            return Ok(variant);
        }

        [HttpPost]
        public async Task<IActionResult> AddAsync([FromBody] AddVariantRequest req)
        {
            Result<KVariant> result = await variantService.AddAsync(req.CharacterId, req.AnimeId, req.Rarity, req.Title);

            return result.Success ? Ok(result.Value) : Conflict(result.Error);
        }

        [HttpPatch("{id}/rarity")]
        public async Task<IActionResult> SetRarityAsync(int id, [FromBody] KRarity newRarity)
        {
            KResult result = await variantService.SetRarityAsync(id, newRarity);

            return result == KResult.Completed ? Ok() : BadRequest(result);
        }

        [HttpPatch("{id}/title")]
        public async Task<IActionResult> SetTitleAsync(int id, [FromBody] string newTitle)
        {
            KResult result = await variantService.SetTitleAsync(id, newTitle);

            return result == KResult.Completed ? Ok() : BadRequest(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            KResult result = await variantService.DeleteAsync(id);

            return result == KResult.Completed ? Ok() : NotFound(result);
        }

        public record AddVariantRequest(int CharacterId, int AnimeId, KRarity Rarity, string Title);
    }
}
