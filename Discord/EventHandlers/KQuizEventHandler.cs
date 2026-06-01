using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using KurisuBot.Core.Infrastructure.Enums;
using KurisuBot.Core.Models;
using KurisuBot.Discord.EmbedBuilders;
using KurisuBot.Engine;
using System.Collections.Concurrent;

namespace KurisuBot.Discord.EventHandlers
{
    internal sealed class KQuizEventHandler
    {
        private static readonly ConcurrentDictionary<ulong, (KQuiz quiz, KUser owner)> _activeQuizzes = new();

        public static async Task<DiscordMessage> RegisterAndSendAsync(CommandContext ctx, KQuiz quiz, KUser owner)
        {
            DiscordEmbed embed = KQuizEmbedBuilder.BuildQuestionEmbed(quiz, owner);

            DiscordMessageBuilder message = new DiscordMessageBuilder()
                .AddEmbed(embed)
                .AddActionRowComponent(KQuizEmbedBuilder.GetButtons(quiz));

            DiscordMessage sentMessage = await ctx.EditResponseAsync(message);
            _activeQuizzes.TryAdd(sentMessage.Id, (quiz, owner));
            int timeout = owner.Settings.QuizTimeToAnswer;
            _ = HandleTimeoutAsync(sentMessage, quiz, owner, timeout);

            return sentMessage;
        }

        public static async Task QuizInteractionAsync(DiscordClient sender, ComponentInteractionCreatedEventArgs e)
        {
            if (!e.Id.StartsWith("multipleChoice")) return;

            if (!_activeQuizzes.TryGetValue(e.Message.Id, out var entry)) return;

            var (quiz, owner) = entry;

            if (!await DiscordEventHandlerHelper.ValidateInputAuthAsync(e, owner)) return;

            _activeQuizzes.TryRemove(e.Message.Id, out _);
            var (selectedOption, correct, reward) = HandleInput(quiz, owner, e.Id);
            DiscordEmbed embed = KQuizEmbedBuilder.BuildResultEmbed(quiz, owner, selectedOption, correct, reward);

            await e.Interaction.CreateResponseAsync(
                DiscordInteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }

        private static (string selectedOption, bool correct, int reward) HandleInput(KQuiz quiz, KUser owner, string buttonId)
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

            if(reward > 0)
            {
                owner.ModifyBalance(KBalance.Coins, KOperation.Add, reward);
                Program.UserService.MarkDirty(owner);
            }

            return (selectedOption, correct, reward);
        }

        private static async Task HandleTimeoutAsync(DiscordMessage message, KQuiz quiz, KUser owner, int timeoutSeconds)
        {
            await Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));

            if (!_activeQuizzes.TryRemove(message.Id, out _)) return;

            DiscordEmbed embed = KQuizEmbedBuilder.BuildQuestionEmbed(quiz, owner, true);

            await message.ModifyAsync(new DiscordMessageBuilder().AddEmbed(embed));
        }
    }
}
