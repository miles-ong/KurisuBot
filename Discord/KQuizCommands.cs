using DSharpPlus.Commands;
using DSharpPlus.Entities;
using KurisuBot.Core.Infrastructure.Enums;
using KurisuBot.Core.Models;
using KurisuBot.Discord.EmbedBuilders;
using KurisuBot.Discord.EventHandlers;

namespace KurisuBot.Discord
{
    internal sealed class KQuizCommands
    {
        [Command("quiz")]
        public static async Task PlayQuizAsync(
            CommandContext ctx,
            [Parameter("difficulty")] KQuizDifficulty difficulty)
        {
            await ctx.DeferResponseAsync();
            KUser owner = await Program.UserService.GetOrCreateAsync((long)ctx.User.Id);

            if(owner.GetBalance(KBalance.QuizTokens) <= 0)
            {
                DiscordEmbed embed = KUserEmbedBuilder.BuildBalanceEmbed(owner, KBalance.QuizTokens);
                await ctx.EditResponseAsync(new DiscordMessageBuilder().AddEmbed(embed));

                return;
            }

            owner.ModifyBalance(KBalance.QuizTokens, KOperation.Subtract, 1);
            Program.UserService.MarkDirty(owner);
            KQuiz quiz = await Program.QuizGenerator.GenerateAsync(difficulty);
            await KQuizEventHandler.RegisterAndSendAsync(ctx, quiz, owner);
        }
    }
}
