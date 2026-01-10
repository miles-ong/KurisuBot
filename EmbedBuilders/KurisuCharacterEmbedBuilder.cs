using DSharpPlus.Entities;
using System.Text;

namespace KurisuBot.EmbedBuilders
{
    public static class KurisuCharacterEmbedBuilder
    {
        public static DiscordEmbedBuilder BuildCharacterInformationEmbed(
            KurisuCharacter character,
            string? requestUsername = null,
            bool displayId = false,
            bool displayAnime = true,
            bool displayImage = true,
            bool displayVariantsCount = true
        )
        {
            StringBuilder sb = new();

            if (displayId) sb.AppendLine($"**ID:** {character.Id}");
            if (displayAnime) sb.AppendLine($"**Anime:** {string.Join(", ", character.Animes.Select(a => a.Title)).ToString()}");
            if (displayVariantsCount) sb.AppendLine($"**Variants:** {character.Variants}");

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithTitle(character.Name)
                .WithDescription(sb.ToString())
                .WithColor(DiscordColor.Purple);

            if(requestUsername != null)
            {
                embed.WithFooter($"Requested by {requestUsername}");
            }
                
            return embed;
        }
    }
}
