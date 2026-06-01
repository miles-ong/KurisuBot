using DSharpPlus.Entities;
using KurisuBot.Core.Infrastructure.Enums;
using KurisuBot.Core.Models;

namespace KurisuBot.Discord.EmbedBuilders
{
    internal static class KResultEmbedBuilder
    {
        public static DiscordEmbed BuildFailEmbed(KResult result, KUser? user, string commandMessage)
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithTitle("Command failed!")
                .WithDescription($"{commandMessage} Error: **{result.ToString()}**")
                .WithColor(DiscordEmbedHelper.KColorToDiscordColor(KColor.Black));

            if (user != null) builder.WithFooter($"Requested by {user.Username}");

            return builder.Build();
        }
    }
}
