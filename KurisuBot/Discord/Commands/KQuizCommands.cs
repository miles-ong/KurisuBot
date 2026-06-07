using DSharpPlus.Commands;
using DSharpPlus.Entities;
using KurisuBot.Discord.EmbedBuilders;
using KurisuBot.Discord.EventHandlers;
using KurisuBot.Quiz;
using KurisuBot.Quiz.Enums;
using KurisuCore.Enums;
using KurisuCore.Models;

namespace KurisuBot.Discord.Commands
{
    internal sealed class KQuizCommands : KCommandBase
    {
        [Command("quiz")]
        public static async Task PlayQuizCommand(
            CommandContext ctx,
            [Parameter("difficulty")] KQuizDifficulty difficulty)
        {
            await ctx.DeferResponseAsync();
            KUser? owner = await GetUserAsync(ctx.User);

            if (owner is null)
            {
                await ctx.EditResponseAsync(new DiscordMessageBuilder()
                    .AddEmbed(KEmbedBuilder.BuildFailEmbed(KResult.CannotFindUser, "Quiz failed!")));

                return;
            }

            if (owner.GetBalance(KBalance.QuizTokens) <= 0)
            {
                DiscordEmbed embed = KUserEmbedBuilder.BuildBalanceEmbed(owner, KBalance.QuizTokens);
                await ctx.EditResponseAsync(new DiscordMessageBuilder().AddEmbed(embed));

                return;
            }

            await Program.ApiClient.User.ModifyBalanceAsync(owner.Id, KBalance.QuizTokens, KOperation.Subtract, 1);
            KQuiz quiz = await Program.QuizGenerator.GenerateAsync(difficulty);
            await KQuizEventHandler.RegisterAndSendAsync(ctx, quiz, owner);
        }
    }
}
