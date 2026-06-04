using DSharpPlus.Entities;
using KurisuCore.Enums;
using KurisuCore.Models;

namespace KurisuBot.Discord.EmbedBuilders
{
    internal static class KEmbedBuilder
    {
        public static DiscordEmbedBuilder Create(
            string title,
            string description,
            KColor color = KColor.White,
            string? footer = null,
            string? imageUrl = null)
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithTitle(title)
                .WithDescription(description)
                .WithColor(new DiscordColor(color.ToHex()));

            if (!string.IsNullOrWhiteSpace(footer)) builder.WithFooter(footer);
            if (!string.IsNullOrWhiteSpace(imageUrl)) builder.WithImageUrl(imageUrl);

            return builder;
        }

        public static void RequestedBy(this DiscordEmbedBuilder builder, KUser? user)
        {
            if (user is null) return;

            builder.WithFooter($"Requested by {user.Username}");
        }

        public static DiscordEmbed BuildFailEmbed(KResult result, string commandMessage, KUser? reqUser = null)
        {
            DiscordEmbedBuilder builder = Create(
                "Command failed!",
                $"{commandMessage} Error: **{result}**",
                KColor.Red
            );

            if (reqUser != null) builder.RequestedBy(reqUser);

            return builder.Build();
        }
    }
}
