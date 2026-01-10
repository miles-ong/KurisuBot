using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System.Collections.Concurrent;

namespace KurisuBot
{
    public struct UniversalQuiz
    {
        public KurisuQuiz Quiz;
        public Dictionary<KurisuUser, bool> UsersAnswers;
    }

    public class KurisuQuizEventHandler
    {
        private static readonly int _universalQuizTimeToAnswer = 45;

        private static ConcurrentDictionary<ulong, KurisuQuiz> _activeQuizzes = new();

        public static async Task<DiscordMessage> RegisterAndSendQuizMessageAsync(CommandContext ctx, KurisuQuiz quiz)
        {
            DiscordEmbedBuilder embed = KurisuQuizEmbedBuilder.BuildQuizEmbed(quiz);

            DiscordMessageBuilder quizMessageContent = new DiscordMessageBuilder()
                .AddEmbed(embed)
                .AddActionRowComponent(KurisuQuizEmbedBuilder.GetQuizButtons(quiz.Type));

            DiscordMessage message = await ctx.EditResponseAsync(quizMessageContent);

            _activeQuizzes.TryAdd(message.Id, quiz);
            int timeoutLength = quiz.Owner != null ? quiz.Owner.Settings.QuizTimeToAnswer : _universalQuizTimeToAnswer;
            _ = HandleQuizTimeoutAsync(message, timeoutLength);

            return message;
        }

        public static async Task QuizInteractionAsync(DiscordClient sender, ComponentInteractionCreatedEventArgs e)
        {
            if (!e.Id.StartsWith("multipleChoice") &&
                !e.Id.StartsWith("trueOrFalse") &&
                !e.Id.StartsWith("higherOrLower"))
            {
                return;
            }

            if(!_activeQuizzes.TryGetValue(e.Message.Id, out KurisuQuiz targetQuiz))
            {
                return;
            }

            if(targetQuiz.Owner == null)
            {
                // Universal quizzes (implement later)
                KurisuUser user = await KurisuUserCache.GetUserAsync((long)e.User.Id);
                throw new NotImplementedException();
            }
            else if(await UniversalEventHandler.ValidateInputAuthAsync(e, targetQuiz.Owner, "This quiz is not for you!"))
            {
                _activeQuizzes.TryRemove(e.Message.Id, out _);

                var (selectedOption, correct, reward) = HandleInput(targetQuiz, e.Id);

                DiscordEmbedBuilder embed = KurisuQuizEmbedBuilder.BuildQuizResultEmbed(targetQuiz, selectedOption, correct, reward);

                DiscordInteractionResponseBuilder response = new DiscordInteractionResponseBuilder()
                    .AddEmbed(embed);

                await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, response);
            }
        }

        private static (string, bool, int) HandleInput(KurisuQuiz quiz, string buttonId, KurisuUser? other = null)
        {
            string selectedOption = buttonId switch
            {
                "multipleChoiceA" => quiz.Options[0],
                "multipleChoiceB" => quiz.Options[1],
                "multipleChoiceC" => quiz.Options[2],
                "multipleChoiceD" => quiz.Options[3],
                "trueOrFalseTrue" => quiz.Options[0],
                "trueOrFalseFalse" => quiz.Options[1],
                "higherOrLowerHigher" => quiz.Options[0],
                "higherOrLowerLower" => quiz.Options[1],
                _ => quiz.Options[0]
            };

            bool correct = selectedOption.Equals(quiz.Answer);
            int reward = quiz.GenerateReward(correct, other);

            return (selectedOption, correct, reward);
        }

        private static async Task HandleQuizTimeoutAsync(DiscordMessage message, int timeoutLength)
        {
            await Task.Delay(TimeSpan.FromSeconds(timeoutLength));

            if(_activeQuizzes.TryRemove(message.Id, out KurisuQuiz quiz))
            {
                DiscordEmbedBuilder embed = KurisuQuizEmbedBuilder.BuildQuizEmbed(quiz, true);

                DiscordMessageBuilder timeoutMessageContent = new DiscordMessageBuilder()
                    .AddEmbed(embed);

                await message.ModifyAsync(timeoutMessageContent);
            }
        }
    }
}
