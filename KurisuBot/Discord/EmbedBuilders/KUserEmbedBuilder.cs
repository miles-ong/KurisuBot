using DSharpPlus.Entities;
using KurisuCore.Enums;
using KurisuCore.Models;

namespace KurisuBot.Discord.EmbedBuilders
{
    internal static class KUserEmbedBuilder
    {
        public static DiscordEmbed BuildWalletEmbed(KUser user, KUser? reqUser = null)
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

            DiscordEmbedBuilder builder = KEmbedBuilder.Create(
                $"{user.Username}'s Wallet",
                description
            );

            builder.RequestedBy(reqUser ?? user);

            return builder.Build();
        }

        public static DiscordEmbed BuildBalanceEmbed(KUser user, KBalance balance, KUser? reqUser = null)
        {
            string description = $"{balance.ToDisplayString()}: **{user.GetBalance(balance)}**";

            if(balance == KBalance.QuizTokens)
            {
                TimeSpan nextRegen = user.GetNextQuizRegen();

                if (nextRegen != TimeSpan.Zero) description += $"\nNext Token in **{nextRegen.Minutes}** minutes!";
            }

            DiscordEmbedBuilder builder = KEmbedBuilder.Create(
                $"{user.Username}'s Wallet",
                description
            );

            builder.RequestedBy(reqUser ?? user);

            return builder.Build();
        }
    }
}
