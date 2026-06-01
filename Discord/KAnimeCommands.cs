using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using KurisuBot.Core.Infrastructure.Enums;
using KurisuBot.Core.Models;
using KurisuBot.Discord.AutoCompleters;
using KurisuBot.Discord.EmbedBuilders;

namespace KurisuBot.Discord
{
    [Command("anime")]
    internal sealed class KAnimeCommands
    {
        [Command("add")]
        public static async Task AddAnimeAsync(
            CommandContext ctx,
            [Parameter("title")] string title,
            [Parameter("description")] string? description = null)
        {
            await ctx.DeferResponseAsync();
            KUser user = await Program.UserService.GetOrCreateAsync((long)ctx.User.Id);
            Result<KAnime> result = await Program.AnimeService.AddAsync(title, description);

            if (!result.Success)
            {
                DiscordEmbed failEmbed = KResultEmbedBuilder.BuildFailEmbed(result.Error, user, "Add anime failed!");
                await ctx.EditResponseAsync(new DiscordMessageBuilder().AddEmbed(failEmbed));
            }

            KAnime anime = result.Value;
            DiscordEmbed embed = KAnimeEmbedBuilder.BuildAnimeEmbed(anime);
            await ctx.EditResponseAsync(new DiscordMessageBuilder().AddEmbed(embed));
        }

        [Command("search")]
        public static async Task SearchAnimeAsync(
            CommandContext ctx,
            [Parameter("title")][SlashAutoCompleteProvider(typeof(KAnimeAutoCompleteProvider))] int id)
        {
            await ctx.DeferResponseAsync();
            KUser user = await Program.UserService.GetOrCreateAsync((long)ctx.User.Id);

            if (!Program.AnimeService.TryGet(id, out KAnime? anime))
            {
                await ctx.EditResponseAsync("Error...");
                return;
            }

            DiscordEmbed embed = KAnimeEmbedBuilder.BuildAnimeEmbed(anime!);
            await ctx.EditResponseAsync(new DiscordMessageBuilder().AddEmbed(embed));
        }
    }
}
