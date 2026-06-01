using KurisuBot.Core.Infrastructure.Enums;

namespace KurisuBot.Core.Models
{
    internal sealed class KQuiz
    {
        public string Question { get; init; } = "";
        public string[] Options { get; init; } = Array.Empty<string>();
        public string Answer { get; init; } = "";
        public KQuizType Type { get; init; }
        public KQuizDifficulty Difficulty { get; init; }
        public string? ImageUrl { get; init; }

        public bool IsCorrect(string selectedOption)
        {
            return selectedOption.Equals(Answer);
        }

        public int GenerateReward(bool correct, int rewardMin, int rewardMax)
        {
            int reward = Random.Shared.Next(rewardMin, rewardMax + 1);

            if (!correct) return reward;

            float mult = Difficulty switch
            {
                KQuizDifficulty.Easy => 1.5f,
                KQuizDifficulty.Medium => 2.0f,
                KQuizDifficulty.Hard => 3.0f,
                _ => 1.0f
            };

            return (int)(reward * mult);
        }
    }
}
