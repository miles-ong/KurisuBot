using DSharpPlus.Commands;
using DSharpPlus.Entities;
using KurisuBot.Discord.EmbedBuilders;
using KurisuCore.Models;
using KurisuCore.Result;

namespace KurisuBot.Discord.Commands
{
    internal sealed class KUserCommands : KCommandBase
    {
        [Command("wallet")]
        public static async Task UserWalletCommand(
            CommandContext ctx,
            [Parameter("user")] DiscordUser? dUser = null
        )
        {
            await ctx.DeferResponseAsync();
            KUser? reqUser = await GetUserAsync(ctx.User);
            KUser? user = await GetUserAsync(dUser ?? ctx.User);

            if (user is null)
            {
                await ctx.EditResponseAsync(new DiscordMessageBuilder()
                    .AddEmbed(KEmbedBuilder.BuildFailEmbed(KDetail.CannotFindUser, "Wallet failed!", reqUser)));

                return;
            }

            DiscordEmbed embed = KUserEmbedBuilder.BuildWalletEmbed(user, reqUser);
            await ctx.EditResponseAsync(new DiscordMessageBuilder().AddEmbed(embed));
        }
    }
}
