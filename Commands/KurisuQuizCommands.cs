using DSharpPlus.Commands;
using KurisuBot.EventHandlers;

namespace KurisuBot.Commands
{
    [Command("quiz")]
    public class KurisuQuizCommands
    {
        [Command("question")]
        public async Task KurisuQuizQuestionCommand(
            CommandContext ctx,
            [Parameter("difficulty")] QuizDifficulty difficulty
        )
        {
            await ctx.DeferResponseAsync();

            KurisuUser user = await KurisuUserCache.SearchByCommandContextAsync(ctx);

            if(user.GetBalance(UserBalance.QuizTokens) <= 0)
            {
                await ctx.EditResponseAsync("Not enough quiz tokens");
                return;
            }

            user.ModifyBalance(UserBalance.QuizTokens, OperationType.Subtract, 1);
            KurisuQuiz quiz = await KurisuQuizGenerator.GenerateAsync(difficulty, user);

            await KurisuQuizEventHandler.RegisterAndSendQuizMessageAsync(ctx, quiz);
        }
    }
}
