using DSharpPlus.Commands;
using DSharpPlus.Entities;
using KurisuBot.Core.Models;
using KurisuBot.Discord.EmbedBuilders;

namespace KurisuBot.Discord
{
    internal sealed class KUserCommands
    {
        [Command("wallet")]
        public static async Task UserWalletAsync(
            CommandContext ctx,
            [Parameter("user")] DiscordUser? dUser = null
        )
        {
            await ctx.DeferResponseAsync();
            long userId = dUser != null ? (long)dUser.Id : (long)ctx.User.Id;
            KUser user = await Program.UserService.GetOrCreateAsync(userId);
            DiscordEmbed embed = KUserEmbedBuilder.BuildWalletEmbed(user);
            await ctx.EditResponseAsync(new DiscordMessageBuilder().AddEmbed(embed));
        }
    }
}
