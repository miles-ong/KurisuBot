using DSharpPlus.Entities;
using KurisuBot.Core.Infrastructure.Enums;
using KurisuBot.Core.Models;

namespace KurisuBot.Discord.EmbedBuilders
{
    internal static class KQuizEmbedBuilder
    {
        public static DiscordEmbed BuildQuestionEmbed(KQuiz quiz, KUser owner, bool timedOut = false)
        {
            string options = quiz.Type switch
            {
                KQuizType.MultipleChoice => $"**A:** {quiz.Options[0]}\n**B:** {quiz.Options[1]}\n**C:** {quiz.Options[2]}\n**D:** {quiz.Options[3]}",
                _ => ""
            };

            string description = $"{options} {(timedOut ? "\n\n:alarm_clock: Time's up! No answer was given..." : "")}";

            DiscordColor color = timedOut
                ? DiscordEmbedHelper.KColorToDiscordColor(KColor.Orange)
                : quiz.Difficulty switch
                {
                    KQuizDifficulty.Easy => DiscordEmbedHelper.KColorToDiscordColor(KColor.Green),
                    KQuizDifficulty.Medium => DiscordEmbedHelper.KColorToDiscordColor(KColor.Yellow),
                    KQuizDifficulty.Hard => DiscordEmbedHelper.KColorToDiscordColor(KColor.Red),
                    _ => DiscordEmbedHelper.KColorToDiscordColor(KColor.Black)
                };

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithTitle(quiz.Question)
                .WithDescription(description)
                .WithColor(color)
                .WithFooter($"{quiz.Difficulty} question requested by {owner.Username}");

            if (!string.IsNullOrWhiteSpace(quiz.ImageUrl)) builder.WithImageUrl(quiz.ImageUrl);

            return builder.Build();
        }

        public static DiscordEmbed BuildResultEmbed(KQuiz quiz, KUser owner, string selectedOption, bool correct, int reward)
        {
            string description = $"{owner.Username} answered **{selectedOption}** {(correct ? "!" : "...")}\n\n"
                   + (correct ? ":white_check_mark: Correct!" : $":x: Wrong! The correct answer was **{quiz.Answer}**!")
                   + $"\n\n{owner.Username} has been awarded **{reward}** coins!";

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithTitle(quiz.Question)
                .WithDescription(description)
                .WithColor(correct ? DiscordEmbedHelper.KColorToDiscordColor(KColor.Green) : DiscordEmbedHelper.KColorToDiscordColor(KColor.Red))
                .WithFooter($"{owner.Username} has {owner.GetBalance(KBalance.QuizTokens)} quiz tokens remaining");

            if (!string.IsNullOrWhiteSpace(quiz.ImageUrl)) builder.WithImageUrl(quiz.ImageUrl);

            return builder.Build();
        }

        public static DiscordButtonComponent[] GetButtons(KQuiz quiz)
        {
            return quiz.Type switch
            {
                KQuizType.MultipleChoice =>
                [
                    new DiscordButtonComponent(DiscordButtonStyle.Primary, "multipleChoiceA", "A"),
                    new DiscordButtonComponent(DiscordButtonStyle.Primary, "multipleChoiceB", "B"),
                    new DiscordButtonComponent(DiscordButtonStyle.Primary, "multipleChoiceC", "C"),
                    new DiscordButtonComponent(DiscordButtonStyle.Primary, "multipleChoiceD", "D"),
                ],
                _ => Array.Empty<DiscordButtonComponent>()
            };
        }
    }
}
