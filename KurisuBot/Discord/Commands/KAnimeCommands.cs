using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using KurisuBot.Discord.AutoCompleters;
using KurisuBot.Discord.EmbedBuilders;
using KurisuCore.Enums;
using KurisuCore.Models;

namespace KurisuBot.Discord.Commands
{
    [Command("anime")]
    internal sealed class KAnimeCommands : KCommandBase
    {
        [Command("search")]
        public static async Task SearchAnimeCommand(
            CommandContext ctx,
            [Parameter("title")][SlashAutoCompleteProvider(typeof(KAnimeAutoCompleteProvider))] int id)
        {
            await ctx.DeferResponseAsync();
            KUser? user = await GetUserAsync(ctx.User);
            KAnime? anime = await Program.APIClient.Anime.GetAsync(id);

            if (anime is null)
            {
                await ctx.EditResponseAsync(new DiscordMessageBuilder()
                    .AddEmbed(KEmbedBuilder.BuildFailEmbed(KResult.CannotFindAnime, "Search failed!", user)));

                return;
            }

            DiscordEmbed embed = await KAnimeEmbedBuilder.BuildAnimeEmbedAsync(anime, user);
            await ctx.EditResponseAsync(new DiscordMessageBuilder().AddEmbed(embed));
        }
    }
}
