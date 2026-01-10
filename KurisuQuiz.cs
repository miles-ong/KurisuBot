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
        public KurisuUser? Owner { get; private set; }
        public string Question { get; private set; } = "";
        public string[] Options { get; private set; } = Array.Empty<string>();
        public string Answer { get; private set; } = "";
        public QuizDifficulty Difficulty { get; private set; }
        public QuizType Type { get; private set; }
        public string? ImageUrl { get; private set; }

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
        private static readonly Dictionary<QuizDifficulty, float> _multiplierAmounts = new()
        {
            { QuizDifficulty.Easy, 1f },
            { QuizDifficulty.Medium, 1.5f },
            { QuizDifficulty.Hard, 2f }
        };
        private static readonly int _descriptionWordLimit = 20;
        private static readonly string[] _genres =
        {
            "Drama", "Action", "Comedy", "Fantasy", "Adventure",
            "Romance", "Slice of Life", "Supernatural", "Sci-Fi",
            "Sports", "Horror"
        };

        public KurisuQuiz(QuizDifficulty difficulty, KurisuUser? user = null)
        {
            Owner = user;
            Difficulty = difficulty;

            int randomQuizVariation = Random.Shared.Next(5);

            switch (randomQuizVariation)
            {
                case 0: _ = GenerateGuessTheAnimeByImageAsync(); break;
                case 1: _ = GenerateGuessTheAnimeByCharactersAsync(); break;
                case 2: _ = GenerateGuessTheAnimeByCharacterImageAsync(); break;
                case 3: _ = GenerateGuessTheAnimeByDescriptionAsync(); break;
                case 4: _ = GenerateGuessTheCharacterByImageAsync(); break;
                default: _ = GenerateGuessTheAnimeByImageAsync(); break;
            }

            Console.WriteLine($"[KurisuQuiz] Generated quiz {(Owner != null ? $"for {Owner.Username}" : "")}: {Question}");
        }

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
            KurisuUser? targetUser = Owner != null ? Owner : other;

            if(targetUser == null)
            {
                return 0;
            }

            int quizRewardMin = targetUser.Settings.QuizRewardMin;
            int quizRewardMax = targetUser.Settings.QuizRewardMax;
            int reward = Random.Shared.Next(quizRewardMin, quizRewardMax + 1);

            if(correct)
            {
                reward = (int)(reward * _multiplierAmounts[Difficulty]);
            }

            targetUser.ModifyBalance(UserBalance.Coins, OperationType.Add, reward);

            return reward;
        }

        private async Task GenerateGuessTheAnimeByImageAsync()
        {
            int randomPageIndex = GetRandomPageIndex();
            string genre = _genres[Random.Shared.Next(_genres.Length)];

            AniListAnimePage page = await AniListHook.GetAnimePageInfoAsync(randomPageIndex, genre);
            AniListAnime[] randomAnimes = AniListHook.GetRandomAnimes(page, 4);
            AniListAnime answer = randomAnimes[Random.Shared.Next(4)];

            Question = "Which anime is this?";
            Options = randomAnimes.Select(a => a.Title).ToArray();
            Answer = answer.Title;
            Type = QuizType.MultipleChoice;
            ImageUrl = answer.ImageUrl;
        }

        private async Task GenerateGuessTheAnimeByCharactersAsync()
        {
            int randomPageIndex = GetRandomPageIndex();
            int characterRange = _difficultyToCharacterRange[Difficulty];
            string genre = _genres[Random.Shared.Next(_genres.Length)];

            AniListAnimePage page = await AniListHook.GetAnimePageInfoAsync(randomPageIndex, genre);
            AniListAnime[] randomAnimes = AniListHook.GetRandomAnimes(page, 4);
            AniListAnime answer = randomAnimes[Random.Shared.Next(4)];
            var (source, characters) = AniListHook.GetRandomCharactersFromAnime(answer, 4, characterRange);

            StringBuilder sb = new();
            sb.Append("Which anime features the following characters?\n");

            foreach(AniListCharacter character in characters)
            {
                sb.AppendLine($"*{character.Name}*");
            }

            Question = sb.ToString();
            Options = randomAnimes.Select(a => a.Title).ToArray();
            Answer = answer.Title;
            Type = QuizType.MultipleChoice;
        }

        private async Task GenerateGuessTheAnimeByCharacterImageAsync()
        {
            int randomPageIndex = GetRandomPageIndex();
            int characterRange = _difficultyToCharacterRange[Difficulty];
            string genre = _genres[Random.Shared.Next(_genres.Length)];

            AniListAnimePage page = await AniListHook.GetAnimePageInfoAsync(randomPageIndex, genre);
            AniListAnime[] randomAnimes = AniListHook.GetRandomAnimes(page, 4);
            AniListAnime answer = randomAnimes[Random.Shared.Next(4)];
            var (source, character) = AniListHook.GetRandomCharacterFromAnime(answer, characterRange);

            Question = "Which anime is this character from?";
            Options = randomAnimes.Select(a => a.Title).ToArray();
            Answer = answer.Title;
            Type = QuizType.MultipleChoice;
            ImageUrl = character.ImageUrl;
        }

        private async Task GenerateGuessTheAnimeByDescriptionAsync()
        {
            int randomPageIndex = GetRandomPageIndex();
            string genre = _genres[Random.Shared.Next(_genres.Length)];

            AniListAnimePage page = await AniListHook.GetAnimePageInfoAsync(randomPageIndex, genre);
            AniListAnime[] randomAnimes = AniListHook.GetRandomAnimes(page, 4);
            AniListAnime answer = randomAnimes[Random.Shared.Next(4)];

            string[] descriptionWords = answer.Description.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int sliceLength = Math.Min(descriptionWords.Length, _descriptionWordLimit);
            int maxStartIndex = descriptionWords.Length - sliceLength;
            int startIndex = maxStartIndex > 0 ? Random.Shared.Next(maxStartIndex + 1) : 0;
            string[] shortenedDescriptionWords = descriptionWords.Skip(startIndex).Take(sliceLength).ToArray();

            string finalDescription = (startIndex > 0 ? "..." : "") +
                                      string.Join(' ', shortenedDescriptionWords) +
                                      (descriptionWords.Length > sliceLength ? "..." : "");

            Question = $"Which anime best fits this description?\n\n*{finalDescription}*";
            Options = randomAnimes.Select(a => a.Title).ToArray();
            Answer = answer.Title;
            Type = QuizType.MultipleChoice;
        }

        private async Task GenerateGuessTheCharacterByImageAsync()
        {
            int randomPageIndex = GetRandomPageIndex();
            int characterRange = _difficultyToCharacterRange[Difficulty];

            AniListAnimePage page = await AniListHook.GetAnimePageInfoAsync(randomPageIndex);
            AniListAnime randomAnime = AniListHook.GetRandomAnime(page);
            var (source, characters) = AniListHook.GetRandomCharactersFromAnime(randomAnime, 4, characterRange);
            AniListCharacter answer = characters[Random.Shared.Next(4)];

            Question = "Who is this?";
            Options = characters.Select(c => c.Name).ToArray();
            Answer = answer.Name;
            Type = QuizType.MultipleChoice;
            ImageUrl = answer.ImageUrl;
        }

        private int GetRandomPageIndex()
        {
            var(min, max) = _difficultyToPageRange[Difficulty];

            return Random.Shared.Next(min, max + 1);
        }
    }
}
