using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using KurisuBot.Discord.EmbedBuilders;
using KurisuBot.Quiz;
using KurisuCore.Enums;
using KurisuCore.Models;

namespace KurisuBot.Discord.EventHandlers
{
    internal sealed class KQuizEventHandler : KEventHandlerBase<(KQuiz quiz, KUser owner)>
    {
        public static async Task<DiscordMessage> RegisterAndSendAsync(CommandContext ctx, KQuiz quiz, KUser owner)
        {
            DiscordEmbed embed = KQuizEmbedBuilder.BuildQuestionEmbed(quiz, owner);

            DiscordMessageBuilder message = new DiscordMessageBuilder()
                .AddEmbed(embed)
                .AddActionRowComponent(KQuizEmbedBuilder.GetButtons(quiz));

            DiscordMessage sentMessage = await ctx.EditResponseAsync(message);
            TryRegister(sentMessage.Id, (quiz, owner));
            int timeout = owner.Settings.QuizTimeToAnswer;

            _ = HandleTimeoutAsync(sentMessage.Id, timeout, async() =>
            {
                DiscordEmbed timeoutEmbed = KQuizEmbedBuilder.BuildQuestionEmbed(quiz, owner, true);
                await sentMessage.ModifyAsync(new DiscordMessageBuilder().AddEmbed(timeoutEmbed));
            });

            return sentMessage;
        }

        public static async Task QuizInteractionAsync(DiscordClient sender, ComponentInteractionCreatedEventArgs e)
        {
            if (!e.Id.StartsWith("multipleChoice")) return;
            if (!TryGet(e.Message.Id, out var entry)) return;

            var (quiz, owner) = entry;

            if (!await ValidateInputAuthAsync(e, owner)) return;

            TryRemove(e.Message.Id, out _);
            var (selectedOption, correct, reward) = await HandleInputAsync(quiz, owner, e.Id);
            DiscordEmbed embed = KQuizEmbedBuilder.BuildResultEmbed(quiz, owner, selectedOption, correct, reward);
            await UpdateMessageAsync(e, embed);
        }

        private static async Task<(string selectedOption, bool correct, int reward)> HandleInputAsync(KQuiz quiz, KUser owner, string buttonId)
        {
            string selectedOption = buttonId switch
            {
                "multipleChoiceA" => quiz.Options[0],
                "multipleChoiceB" => quiz.Options[1],
                "multipleChoiceC" => quiz.Options[2],
                "multipleChoiceD" => quiz.Options[3],
                _ => quiz.Options[0]
            };

            bool correct = selectedOption.Equals(quiz.Answer);
            int reward = quiz.GenerateReward(correct, owner.Settings.QuizRewardMin, owner.Settings.QuizRewardMax);

            if (reward > 0) await Program.ApiClient.User.ModifyBalanceAsync(owner.Id, KBalance.Coins, KOperation.Add, reward);

            return (selectedOption, correct, reward);
        }
    }
}
