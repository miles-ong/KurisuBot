using System.Text;

namespace KurisuBot
{
    public enum QuizDifficulty
    {
        Easy,
        Medium,
        Hard
    }

    public enum QuizType
    {
        MultipleChoice,
        TrueOrFalse,
        HigherOrLower
    }

    public class KurisuQuiz
    {
        public KurisuUser? Owner { get; set; }
        public string Question { get; set; } = "";
        public string[] Options { get; set; } = Array.Empty<string>();
        public string Answer { get; set; } = "";
        public QuizDifficulty Difficulty { get; set; }
        public QuizType Type { get; set; }
        public string? ImageUrl { get; set; }

        public string GetOptionsString()
        {
            return Type switch
            {
                QuizType.MultipleChoice => $"**A:** {Options[0]}\n" +
                                           $"**B:** {Options[1]}\n" +
                                           $"**C:** {Options[2]}\n" +
                                           $"**D:** {Options[3]}\n",

                QuizType.TrueOrFalse => "Is this statement *true*, or is it *false*?",

                QuizType.HigherOrLower => "*Based on AniList's data.*",

                _ => ""
            };
        }

        public int GenerateReward(bool correct, KurisuUser? other = null)
        {
            KurisuUser? targetUser = Owner ?? other;

            if (targetUser == null)
            {
                return 0;
            }

            int quizRewardMin = targetUser.Settings.QuizRewardMin;
            int quizRewardMax = targetUser.Settings.QuizRewardMax;
            int reward = Random.Shared.Next(quizRewardMin, quizRewardMax + 1);

            if (correct)
            {
                Dictionary<QuizDifficulty, float> multipliers = new()
                {
                    { QuizDifficulty.Easy, 1f },
                    { QuizDifficulty.Medium, 1.5f },
                    { QuizDifficulty.Hard, 2f }
                };

                reward = (int)(reward * multipliers[Difficulty]);
            }

            targetUser.ModifyBalance(UserBalance.Coins, OperationType.Add, reward);

            return reward;
        }
    }

    public static class KurisuQuizGenerator
    {
        private static readonly Dictionary<QuizDifficulty, (int min, int max)> _difficultyToPageRange = new()
        {
            { QuizDifficulty.Easy, (1, 2) },
            { QuizDifficulty.Medium, (2, 5) },
            { QuizDifficulty.Hard, (5, 10) }
        };
        private static readonly Dictionary<QuizDifficulty, int> _difficultyToCharacterRange = new()
        {
            { QuizDifficulty.Easy, 10 },
            { QuizDifficulty.Medium, 20 },
            { QuizDifficulty.Hard, 30 }
        };
        private static readonly int _descriptionWordLimit = 20;
        private static readonly string[] _genres =
        {
            "Drama", "Action", "Comedy", "Fantasy", "Adventure",
            "Romance", "Slice of Life", "Supernatural", "Sci-Fi",
            "Sports", "Horror"
        };

        public static async Task<KurisuQuiz> GenerateAsync(QuizDifficulty difficulty, KurisuUser? user = null)
        {
            KurisuQuiz quiz = new()
            {
                Owner = user,
                Difficulty = difficulty
            };

            int randomQuizVariation = Random.Shared.Next(5);

            switch (randomQuizVariation)
            {
                case 0: await GenerateGuessTheAnimeByImageAsync(quiz); break;
                case 1: await GenerateGuessTheAnimeByCharactersAsync(quiz); break;
                case 2: await GenerateGuessTheAnimeByCharacterImageAsync(quiz); break;
                case 3: await GenerateGuessTheAnimeByDescriptionAsync(quiz); break;
                case 4: await GenerateGuessTheCharacterByImageAsync(quiz); break;
                default: await GenerateGuessTheAnimeByImageAsync(quiz); break;
            }

            Console.WriteLine($"[KurisuQuiz] Generated quiz {(quiz.Owner != null ? $"for {quiz.Owner.Username}" : "")}: {quiz.Question}");

            return quiz;
        }

        private static async Task GenerateGuessTheAnimeByImageAsync(KurisuQuiz quiz)
        {
            int randomPageIndex = GetRandomPageIndex(quiz.Difficulty);
            string genre = _genres[Random.Shared.Next(_genres.Length)];

            AniListAnimePage page = await AniListHook.GetAnimePageInfoAsync(randomPageIndex, genre);
            AniListAnime[] randomAnimes = AniListHook.GetRandomAnimes(page, 4);
            AniListAnime answer = randomAnimes[Random.Shared.Next(randomAnimes.Length)];

            quiz.Question = "Which anime is this?";
            quiz.Options = randomAnimes.Select(a => a.Title).ToArray();
            quiz.Answer = answer.Title;
            quiz.Type = QuizType.MultipleChoice;
            quiz.ImageUrl = answer.ImageUrl;
        }

        private static async Task GenerateGuessTheAnimeByCharactersAsync(KurisuQuiz quiz)
        {
            int randomPageIndex = GetRandomPageIndex(quiz.Difficulty);
            int characterRange = _difficultyToCharacterRange[quiz.Difficulty];
            string genre = _genres[Random.Shared.Next(_genres.Length)];

            AniListAnimePage page = await AniListHook.GetAnimePageInfoAsync(randomPageIndex, genre);
            AniListAnime[] randomAnimes = AniListHook.GetRandomAnimes(page, 4);
            AniListAnime answer = randomAnimes[Random.Shared.Next(randomAnimes.Length)];
            var (source, characters) = AniListHook.GetRandomCharactersFromAnime(answer, 4, characterRange);

            StringBuilder sb = new();
            sb.Append("Which anime features the following characters?\n\n");

            foreach (AniListCharacter character in characters)
            {
                sb.AppendLine($"*{character.Name}*");
            }

            quiz.Question = sb.ToString();
            quiz.Options = randomAnimes.Select(a => a.Title).ToArray();
            quiz.Answer = answer.Title;
            quiz.Type = QuizType.MultipleChoice;
        }

        private static async Task GenerateGuessTheAnimeByCharacterImageAsync(KurisuQuiz quiz)
        {
            int randomPageIndex = GetRandomPageIndex(quiz.Difficulty);
            int characterRange = _difficultyToCharacterRange[quiz.Difficulty];
            string genre = _genres[Random.Shared.Next(_genres.Length)];

            AniListAnimePage page = await AniListHook.GetAnimePageInfoAsync(randomPageIndex, genre);
            AniListAnime[] randomAnimes = AniListHook.GetRandomAnimes(page, 4);
            AniListAnime answer = randomAnimes[Random.Shared.Next(randomAnimes.Length)];
            var (source, character) = AniListHook.GetRandomCharacterFromAnime(answer, characterRange);

            quiz.Question = "Which anime is this character from?";
            quiz.Options = randomAnimes.Select(a => a.Title).ToArray();
            quiz.Answer = answer.Title;
            quiz.Type = QuizType.MultipleChoice;
            quiz.ImageUrl = character.ImageUrl;
        }

        private static async Task GenerateGuessTheAnimeByDescriptionAsync(KurisuQuiz quiz)
        {
            int randomPageIndex = GetRandomPageIndex(quiz.Difficulty);
            string genre = _genres[Random.Shared.Next(_genres.Length)];

            AniListAnimePage page = await AniListHook.GetAnimePageInfoAsync(randomPageIndex, genre);
            AniListAnime[] randomAnimes = AniListHook.GetRandomAnimes(page, 4);
            AniListAnime answer = randomAnimes[Random.Shared.Next(randomAnimes.Length)];

            string[] descriptionWords = answer.Description.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int sliceLength = Math.Min(descriptionWords.Length, _descriptionWordLimit);
            int maxStartIndex = descriptionWords.Length - sliceLength;
            int startIndex = maxStartIndex > 0 ? Random.Shared.Next(maxStartIndex + 1) : 0;
            string[] shortenedDescriptionWords = descriptionWords.Skip(startIndex).Take(sliceLength).ToArray();

            string finalDescription = (startIndex > 0 ? "..." : "") +
                                      string.Join(' ', shortenedDescriptionWords) +
                                      (descriptionWords.Length > sliceLength ? "..." : "");

            quiz.Question = $"Which anime best fits this description?\n\n*{finalDescription}*";
            quiz.Options = randomAnimes.Select(a => a.Title).ToArray();
            quiz.Answer = answer.Title;
            quiz.Type = QuizType.MultipleChoice;
        }

        private static async Task GenerateGuessTheCharacterByImageAsync(KurisuQuiz quiz)
        {
            int randomPageIndex = GetRandomPageIndex(quiz.Difficulty);
            int characterRange = _difficultyToCharacterRange[quiz.Difficulty];

            AniListAnimePage page = await AniListHook.GetAnimePageInfoAsync(randomPageIndex);
            AniListAnime randomAnime = AniListHook.GetRandomAnime(page);
            var (source, characters) = AniListHook.GetRandomCharactersFromAnime(randomAnime, 4, characterRange);
            AniListCharacter answer = characters[Random.Shared.Next(characters.Length)];

            quiz.Question = "Who is this?";
            quiz.Options = characters.Select(c => c.Name).ToArray();
            quiz.Answer = answer.Name;
            quiz.Type = QuizType.MultipleChoice;
            quiz.ImageUrl = answer.ImageUrl;
        }

        private static int GetRandomPageIndex(QuizDifficulty difficulty)
        {
            var (min, max) = _difficultyToPageRange[difficulty];

            return Random.Shared.Next(min, max + 1);
        }
    }
}