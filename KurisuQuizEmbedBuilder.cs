using DSharpPlus.Entities;

namespace KurisuBot
{
    public static class KurisuQuizEmbedBuilder
    {
        public static DiscordEmbedBuilder BuildQuizEmbed(KurisuQuiz quiz, bool timedOut = false)
        {
            string descriptionString = quiz.GetOptionsString() +
                                       (timedOut ? "\n\n:alarm_clock: Time's up! No answer was given..." : "");

            string footerString = timedOut
                ? $"{quiz.Owner!.Username} has {quiz.Owner.GetBalance(UserBalance.QuizTokens)} remaining quiz tokens"
                : $"{quiz.Difficulty.ToString()} question requested by {quiz.Owner!.Username}";

            DiscordEmbedBuilder embed = new()
            {
                Title = quiz.Question,
                Description = descriptionString,
                Color = timedOut ? DiscordColor.Orange : GetQuizDifficultyColor(quiz.Difficulty)
            };

            if(!string.IsNullOrEmpty(quiz.ImageUrl))
            {
                embed.WithImageUrl(quiz.ImageUrl);
            }

            embed.WithFooter(footerString);

            return embed;
        }

        public static DiscordEmbedBuilder BuildQuizResultEmbed(KurisuQuiz quiz, string selectedOption, bool correct, int reward)
        {
            string descriptionString = $"{quiz.Owner!.Username} answered **{selectedOption}** {(correct ? "!" : "...")}\n\n" +
                                       (correct ? $":white_check_mark: Correct!" : $":x: Wrong! The correct answer was **{quiz.Answer}**!") +
                                       $"\n\n{quiz.Owner.Username} has been awarded **{reward}** coins!";

            DiscordEmbedBuilder embed = new()
            {
                Title = quiz.Question,
                Description = descriptionString,
                Color = correct ? DiscordColor.Green : DiscordColor.Red
            };

            if(!string.IsNullOrEmpty(quiz.ImageUrl))
            {
                embed.WithImageUrl(quiz.ImageUrl);
            }

            embed.WithFooter($"{quiz.Owner.Username} has {quiz.Owner.GetBalance(UserBalance.QuizTokens)} remaining quiz tokens");

            return embed;
        }

        public static DiscordButtonComponent[] GetQuizButtons(QuizType type)
        {
            return type switch
            {
                QuizType.MultipleChoice => GetMultipleChoiceButtons(),
                QuizType.TrueOrFalse => GetTrueOrFalseButtons(),
                QuizType.HigherOrLower => GetHigherOrLowerButtons(),
                _ => GetMultipleChoiceButtons()
            };
        }

        private static DiscordButtonComponent[] GetMultipleChoiceButtons()
        {
            DiscordButtonComponent aButton = new DiscordButtonComponent(DiscordButtonStyle.Primary, "multipleChoiceA", "A");
            DiscordButtonComponent bButton = new DiscordButtonComponent(DiscordButtonStyle.Primary, "multipleChoiceB", "B");
            DiscordButtonComponent cButton = new DiscordButtonComponent(DiscordButtonStyle.Primary, "multipleChoiceC", "C");
            DiscordButtonComponent dButton = new DiscordButtonComponent(DiscordButtonStyle.Primary, "multipleChoiceD", "D");

            DiscordButtonComponent[] buttons = { aButton, bButton, cButton, dButton };

            return buttons;
        }

        private static DiscordButtonComponent[] GetTrueOrFalseButtons()
        {
            DiscordButtonComponent tButton = new DiscordButtonComponent(DiscordButtonStyle.Success, "trueOrFalseTrue", "True");
            DiscordButtonComponent fButton = new DiscordButtonComponent(DiscordButtonStyle.Danger, "trueOrFalseFalse", "False");

            DiscordButtonComponent[] buttons = { tButton, fButton };

            return buttons;
        }

        private static DiscordButtonComponent[] GetHigherOrLowerButtons()
        {
            DiscordButtonComponent hButton = new DiscordButtonComponent(DiscordButtonStyle.Success, "higherOrLowerHigher", "Higher");
            DiscordButtonComponent lButton = new DiscordButtonComponent(DiscordButtonStyle.Danger, "higherOrLowerLower", "Lower");

            DiscordButtonComponent[] buttons = { hButton, lButton };

            return buttons;
        }

        private static DiscordColor GetQuizDifficultyColor(QuizDifficulty difficulty)
        {
            return difficulty switch
            {
                QuizDifficulty.Easy => DiscordColor.Green,
                QuizDifficulty.Medium => DiscordColor.Yellow,
                QuizDifficulty.Hard => DiscordColor.Red,
                _ => DiscordColor.Black
            };
        }
    }
}
