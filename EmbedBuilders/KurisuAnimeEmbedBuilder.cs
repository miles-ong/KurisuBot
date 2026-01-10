using DSharpPlus.Entities;
using System.Text;

namespace KurisuBot.EmbedBuilders
{
    public static class KurisuAnimeEmbedBuilder
    {
        public static DiscordEmbedBuilder BuildAnimeInformationEmbed(
            KurisuAnime anime,
            string? requestUsername = null,
            bool displayId = false,
            bool displayDescription = true,
            bool displayImage = true,
            bool displayCharactersCount = true
        )
        {
            StringBuilder sb = new();

            if (displayId) sb.AppendLine($"**ID:** {anime.Id}");
            if (displayCharactersCount) sb.AppendLine($"**Characters:** {anime.Characters.Count}");
            if (displayDescription) sb.AppendLine($"\n{anime.Description}");

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithTitle(anime.Title)
                .WithDescription(sb.ToString())
                .WithColor(DiscordColor.Red);

            if(requestUsername != null)
            {
                embed.WithFooter($"Requested by {requestUsername}");
            }

            return embed;
        }
    }
}
