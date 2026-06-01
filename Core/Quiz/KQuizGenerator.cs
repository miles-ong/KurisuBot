using KurisuBot.Core.Infrastructure.Enums;
using KurisuBot.Core.Models;

namespace KurisuBot.Core.Quiz
{
    internal sealed class KQuizGenerator
    {
        private readonly IQuizDataSource _dataSource;

        private static readonly Dictionary<KQuizDifficulty, (int min, int max)> _pageRanges = new()
        {
            { KQuizDifficulty.Easy, (1, 2) },
            { KQuizDifficulty.Medium, (2, 5) },
            { KQuizDifficulty.Hard, (5, 10) }
        };

        private static readonly Dictionary<KQuizDifficulty, int> _characterRanges = new()
        {
            { KQuizDifficulty.Easy, 10 },
            { KQuizDifficulty.Medium, 20 },
            { KQuizDifficulty.Hard, 30 }
        };

        private static readonly string[] _genres =
        {
            "Drama", "Action", "Comedy", "Fantasy", "Adventure",
            "Romance", "Slice of Life", "Supernatural", "Sci-Fi", "Sports", "Horror"
        };

        public KQuizGenerator(IQuizDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<KQuiz> GenerateAsync(KQuizDifficulty difficulty)
        {
            int variation = Random.Shared.Next(5);

            return variation switch
            {
                0 => await GuessAnimeByImageAsync(difficulty),
                1 => await GuessAnimeByCharacterNamesAsync(difficulty),
                2 => await GuessAnimeByCharacterImageAsync(difficulty),
                3 => await GuessAnimeByDescriptionAsync(difficulty),
                4 => await GuessCharacterByImageAsync(difficulty),
                _ => await GuessAnimeByImageAsync(difficulty)
            };
        }

        private async Task<KQuiz> GuessAnimeByImageAsync(KQuizDifficulty difficulty)
        {
            var (animes, answer) = await GetRandomAnimesAsync(difficulty, 4);

            if (animes.Count != 4) return await GuessAnimeByImageAsync(difficulty);

            return new KQuiz
            {
                Question = "Which anime is this?",
                Options = animes.Select(a => a.Title).ToArray(),
                Answer = answer.Title,
                Type = KQuizType.MultipleChoice,
                Difficulty = difficulty,
                ImageUrl = answer.ImageUrl
            };
        }

        private async Task<KQuiz> GuessAnimeByCharacterNamesAsync(KQuizDifficulty difficulty)
        {
            var (animes, answer) = await GetRandomAnimesAsync(difficulty, 4);
            List<QuizCharacter> characters = GetRandomCharacters(answer, 4, _characterRanges[difficulty]);

            if (characters.Count != 4) return await GuessAnimeByCharacterNamesAsync(difficulty);

            string characterList = string.Join("\n", characters.Select(c => $"*{c.Name}*"));

            return new KQuiz
            {
                Question = $"Which anime features the following characters?\n\n{characterList}",
                Options = animes.Select(a => a.Title).ToArray(),
                Answer = answer.Title,
                Type = KQuizType.MultipleChoice,
                Difficulty = difficulty
            };
        }

        private async Task<KQuiz> GuessAnimeByCharacterImageAsync(KQuizDifficulty difficulty)
        {
            var (animes, answer) = await GetRandomAnimesAsync(difficulty, 4);

            if (animes.Count != 4) return await GuessAnimeByCharacterImageAsync(difficulty);

            QuizCharacter character = GetRandomCharacters(answer, 1, _characterRanges[difficulty])[0];

            return new KQuiz
            {
                Question = "Which anime is this character from?",
                Options = animes.Select(a => a.Title).ToArray(),
                Answer = answer.Title,
                Type = KQuizType.MultipleChoice,
                Difficulty = difficulty,
                ImageUrl = character.ImageUrl
            };
        }

        private async Task<KQuiz> GuessAnimeByDescriptionAsync(KQuizDifficulty difficulty)
        {
            var (animes, answer) = await GetRandomAnimesAsync(difficulty, 4);

            if (animes.Count != 4) return await GuessAnimeByDescriptionAsync(difficulty);

            string description = ShortenDescription(answer.Description ?? "", 20);

            return new KQuiz
            {
                Question = $"Which anime best fits this description?\n\n*{description}*",
                Options = animes.Select(a => a.Title).ToArray(),
                Answer = answer.Title,
                Type = KQuizType.MultipleChoice,
                Difficulty = difficulty
            };
        }

        private async Task<KQuiz> GuessCharacterByImageAsync(KQuizDifficulty difficulty)
        {
            var (animes, anime) = await GetRandomAnimesAsync(difficulty, 1);
            List<QuizCharacter> characters = GetRandomCharacters(anime, 4, _characterRanges[difficulty]);

            if (characters.Count != 4) return await GuessCharacterByImageAsync(difficulty);

            QuizCharacter answer = characters[Random.Shared.Next(characters.Count)];

            return new KQuiz
            {
                Question = "Who is this?",
                Options = characters.Select(c => c.Name).ToArray(),
                Answer = answer.Name,
                Type = KQuizType.MultipleChoice,
                Difficulty = difficulty,
                ImageUrl = answer.ImageUrl
            };
        }

        private async Task<(List<QuizAnime> animes, QuizAnime answer)> GetRandomAnimesAsync(KQuizDifficulty difficulty, int count)
        {
            var (min, max) = _pageRanges[difficulty];
            int pageIndex = Random.Shared.Next(min, max + 1);
            string genre = _genres[Random.Shared.Next(_genres.Length)];
            QuizAnimePool pool = await _dataSource.GetAnimePoolAsync(pageIndex, genre);

            if (pool.Animes.Count < count) pool = await _dataSource.GetAnimePoolAsync(min, genre);

            List<QuizAnime> animes = pool.Animes
                .OrderBy(_ => Random.Shared.Next())
                .Take(count)
                .ToList();

            QuizAnime answer = animes[Random.Shared.Next(animes.Count)];

            return (animes, answer);
        }

        private static List<QuizCharacter> GetRandomCharacters(QuizAnime anime, int count, int maxIndex)
        {
            maxIndex = Math.Min(maxIndex, anime.Characters.Count);

            return anime.Characters
                .Take(maxIndex)
                .OrderBy(_ => Random.Shared.Next())
                .Take(count)
                .ToList();
        }
        private static string ShortenDescription(string description, int wordLimit)
        {
            string[] words = description.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int sliceLength = Math.Min(words.Length, wordLimit);
            int maxStart = words.Length - sliceLength;
            int start = maxStart > 0 ? Random.Shared.Next(maxStart + 1) : 0;

            return (start > 0 ? "..." : "")
                + string.Join(' ', words.Skip(start).Take(sliceLength))
                + (words.Length > sliceLength ? "..." : "");
        }
    }
}
