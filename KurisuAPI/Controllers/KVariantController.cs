using KurisuAPI.Services;
using KurisuCore.Enums;
using KurisuCore.Models;
using KurisuCore.Models.Data;
using KurisuCore.Result;
using Microsoft.AspNetCore.Mvc;

namespace KurisuAPI.Controllers
{
    [ApiController]
    [Route("variants")]
    public sealed class KVariantController(KVariantService variantService) : ControllerBase
    {
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var variant = await variantService.GetByIdAsync(id);

            if (variant is null) return NotFound(KDetail.VariantNotFound);

            return Ok(variant);
        }

        [HttpGet("{id:int}/packs")]
        public async Task<IActionResult> GetPacks(int id)
        {
            var variant = await variantService.GetByIdAsync(id);

            if (variant is null) return NotFound(KDetail.VariantNotFound);

            return Ok(variant.Packs);
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] KVariantData data)
        {
            var result = await variantService.AddAsync(data);

            return result.Success ? Ok(result.Value) : Conflict(result.Detail);
        }

        [HttpPatch("{id:int}/character")]
        public async Task<IActionResult> SetCharacter(int id, [FromBody] KCharacter newCharacter)
        {
            var detail = await variantService.SetCharacterAsync(id, newCharacter);

            return detail == KDetail.Completed ? Ok() : BadRequest(detail);
        }

        [HttpPatch("{id:int}/anime")]
        public async Task<IActionResult> SetAnime(int id, [FromBody] KAnime newAnime)
        {
            var detail = await variantService.SetAnimeAsync(id, newAnime);

            return detail == KDetail.Completed ? Ok() : BadRequest(detail);
        }

        [HttpPatch("{id:int}/rarity")]
        public async Task<IActionResult> SetRarity(int id, [FromBody] KRarity newRarity)
        {
            var detail = await variantService.SetRarityAsync(id, newRarity);

            return detail == KDetail.Completed ? Ok() : BadRequest(detail);
        }

        [HttpPatch("{id:int}/title")]
        public async Task<IActionResult> SetTitle(int id, [FromBody] string newTitle)
        {
            var detail = await variantService.SetTitleAsync(id, newTitle);

            return detail == KDetail.Completed ? Ok() : BadRequest(detail);
        }

        [HttpPatch("{id:int}/artist")]
        public async Task<IActionResult> SetArtist(int id, [FromBody] string newArtist)
        {
            var detail = await variantService.SetArtistAsync(id, newArtist);
            
            return detail == KDetail.Completed ? Ok() : BadRequest(detail);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var detail = await variantService.DeleteAsync(id);

            return detail == KDetail.Completed ? Ok() : NotFound(detail);
        }
    }
}
