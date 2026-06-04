using KurisuAPI.Services;
using KurisuCore.Enums;
using KurisuCore.Models;
using Microsoft.AspNetCore.Mvc;

namespace KurisuAPI.Controllers
{
    [ApiController]
    [Route("users")]
    public class KUserController(KUserService userService) : ControllerBase
    {
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrCreateAsync(long id)
        {
            KUser user = await userService.GetOrCreateAsync(id);

            return Ok(user);
        }

        [HttpPatch("{id}/balance")]
        public async Task<IActionResult> ModifyBalanceAsync(long id, [FromBody] ModifyBalanceRequest req)
        {
            KUser user = await userService.GetOrCreateAsync(id);
            user.ModifyBalance(req.Balance, req.Operation, req.Amount);
            userService.MarkDirty(user);

            return Ok();
        }

        public record ModifyBalanceRequest(KBalance Balance, KOperation Operation, int Amount);
    }
}
