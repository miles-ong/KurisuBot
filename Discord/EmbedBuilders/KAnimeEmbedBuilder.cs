using DSharpPlus.Entities;
using KurisuBot.Core.Infrastructure.Enums;
using KurisuBot.Core.Models;

namespace KurisuBot.Discord.EmbedBuilders
{
    internal static class KAnimeEmbedBuilder
    {
        public static DiscordEmbed BuildAnimeEmbed(KAnime anime)
        {
            string description = $"Characters: **{anime.Characters.Count()}**\n\n" +
                                 anime.Description;

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithTitle(anime.Title)
                .WithDescription(description)
                .WithFooter($"ID: {anime.Id}")
                .WithColor(DiscordEmbedHelper.KColorToDiscordColor(KColor.Red));

            return builder.Build();
        }
    }
}
