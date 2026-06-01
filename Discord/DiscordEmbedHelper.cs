using DSharpPlus.Entities;
using KurisuBot.Core.Infrastructure.Enums;

namespace KurisuBot.Discord
{
    internal static class DiscordEmbedHelper
    {
        public static DiscordColor KColorToDiscordColor(KColor color)
        {
            return new DiscordColor(KColorHelper.KColorToHex(color));
        }
    }
}
