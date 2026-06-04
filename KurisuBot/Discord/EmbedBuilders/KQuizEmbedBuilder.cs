using DSharpPlus.Entities;
using KurisuBot.Quiz;
using KurisuBot.Quiz.Enums;
using KurisuCore.Models;
using KurisuCore.Enums;

namespace KurisuBot.Discord.EmbedBuilders
{
    internal static class KQuizEmbedBuilder
    {
        public static DiscordEmbed BuildQuestionEmbed(KQuiz quiz, KUser owner, bool timedOut = false)
        {
            string options = quiz.Type switch
            {
                KQuizType.MultipleChoice    => $"**A:** {quiz.Options[0]}\n**B:** {quiz.Options[1]}\n**C:** {quiz.Options[2]}\n**D:** {quiz.Options[3]}",
                _                           => ""
            };

            string description = $"{options} {(timedOut ? "\n\n:alarm_clock: Time's up! No answer was given..." : "")}";

            KColor color = timedOut
                ? KColor.Orange
                : quiz.Difficulty switch
                {
                    KQuizDifficulty.Easy    => KColor.Green,
                    KQuizDifficulty.Medium  => KColor.Yellow,
                    KQuizDifficulty.Hard    => KColor.Red,
                    _                       => KColor.Black
                };

            DiscordEmbedBuilder builder = KEmbedBuilder.Create(
                quiz.Question,
                description,
                color,
                $"{quiz.Difficulty} question requested by {owner.Username}",
                quiz.ImageUrl
            );

            return builder.Build();
        }

        public static DiscordEmbed BuildResultEmbed(KQuiz quiz, KUser owner, string selectedOption, bool correct, int reward)
        {
            string description = $"{owner.Username} answered **{selectedOption}** {(correct ? "!" : "...")}\n\n"
                   + (correct ? ":white_check_mark: Correct!" : $":x: Wrong! The correct answer was **{quiz.Answer}**!")
                   + $"\n\n{owner.Username} has been awarded **{reward}** coins!";

            KColor color = correct ? KColor.Green : KColor.Red;

            DiscordEmbedBuilder builder = KEmbedBuilder.Create(
                quiz.Question,
                description,
                color,
                $"{owner.Username} has {owner.GetBalance(KBalance.QuizTokens)} quiz tokens remaining",
                quiz.ImageUrl
            );

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
