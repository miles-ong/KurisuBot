using DSharpPlus.Entities;
using KurisuBot.Core.Infrastructure.Enums;
using KurisuBot.Core.Models;

namespace KurisuBot.Discord.EmbedBuilders
{
    internal static class KUserEmbedBuilder
    {
        public static DiscordEmbed BuildWalletEmbed(KUser user)
        {
            string description = $"""
                Coins: **{user.GetBalance(KBalance.Coins)}**
                R Dust: **{user.GetBalance(KBalance.RDust)}**
                SR Dust: **{user.GetBalance(KBalance.SRDust)}**
                SSR Dust: **{user.GetBalance(KBalance.SSRDust)}**
                UR Dust: **{user.GetBalance(KBalance.URDust)}**

                Quiz Tokens: **{user.GetBalance(KBalance.QuizTokens)}**
            """;

            TimeSpan nextRegen = user.GetNextQuizRegen();

            if (nextRegen != TimeSpan.Zero) description += $"\nNext Token in **{nextRegen.Minutes}** minutes!";

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithTitle($"{user.Username}'s Wallet")
                .WithDescription(description);

            return builder.Build();
        }

        public static DiscordEmbed BuildBalanceEmbed(KUser user, KBalance balance)
        {
            string description = $"{KBalanceHelper.KBalanceToString(balance)}: **{user.GetBalance(balance)}**";

            if(balance == KBalance.QuizTokens)
            {
                TimeSpan nextRegen = user.GetNextQuizRegen();

                if (nextRegen != TimeSpan.Zero) description += $"\nNext Token in **{nextRegen.Minutes}** minutes!";
            }

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithTitle($"{user.Username}'s Wallet")
                .WithDescription(description);

            return builder.Build();
        }
    }
}
