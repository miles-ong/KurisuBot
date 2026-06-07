using DSharpPlus.Entities;
using KurisuCore.Enums;
using KurisuCore.Models;

namespace KurisuBot.Discord.EmbedBuilders
{
    internal static class KAnimeEmbedBuilder
    {
        public static async Task<DiscordEmbed> BuildAnimeEmbedAsync(KAnime anime, KUser? reqUser = null)
        {
            List<KCharacter> characters = await Program.ApiClient.Anime.GetCharactersAsync(anime.Id);

            string description = $"Characters: **{characters.Count}**\n\n" +
                                 anime.Description;

            DiscordEmbedBuilder builder = KEmbedBuilder.Create(
                anime.Title,
                description,
                KColor.Red
            );

            builder.RequestedBy(reqUser);

            return builder.Build();
        }
    }
}
