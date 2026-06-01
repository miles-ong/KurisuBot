namespace KurisuBot.AniList
{
    internal sealed class AniListService
    {
        private readonly AniListClient _client;
        private const string _consoleTag = "[AniListService]";

        public AniListService(HttpClient? httpClient = null)
        {
            _client = new AniListClient(httpClient);
        }

        public async Task InitializeAsync()
        {
            await GetAnimePageAsync(1);
            Console.WriteLine($"{_consoleTag} Initialized!");
        }

        public async Task<AniListAnimePage> GetAnimePageAsync(int pageNumber, string? genre = null, string sortFilter = "POPULARITY_DESC")
        {
            string key = $"page_g:{genre ?? "all"}_pn:{pageNumber}_sf:{sortFilter}".ToLowerInvariant();

            if (AniListCache.TryGetPage(key, out AniListAnimePage? cached)) return cached!;

            try
            {
                List<AniListAnime> animes = await _client.GetAnimePageAsync(pageNumber, genre, sortFilter);

                foreach(AniListAnime anime in animes)
                {
                    string animeKey = $"anime_{anime.Id}";

                    if (!AniListCache.TryGetAnime(animeKey, out _)) AniListCache.SetAnime(animeKey, anime);
                }

                AniListAnimePage page = new()
                {
                    Genre = genre ?? "all",
                    PageNumber = pageNumber,
                    SortFilter = sortFilter,
                    Animes = animes
                };

                AniListCache.SetPage(key, page);
                Console.WriteLine($"{_consoleTag} Successfully cached page '{genre ?? "all"}_{pageNumber}_{sortFilter}'");

                return page;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{_consoleTag} Failed to fetch page, using fallback: {ex.Message}");

                return AniListCache.GetRandomPage() ?? throw new Exception($"No fallback page available in cache!");
            }
        }

        public AniListAnime[] GetRandomAnimes(AniListAnimePage page, int count, int maxIndex = 50)
        {
            if(page.Animes.Count < count)
            {
                Console.WriteLine($"{_consoleTag} Page too small, using fallback...");
                AniListAnimePage fallback = AniListCache.GetRandomPage() ?? throw new Exception("No fallback page available.");

                return GetRandomAnimes(fallback, count, maxIndex);
            }

            maxIndex = Math.Min(maxIndex, page.Animes.Count);
            HashSet<int> usedIndices = new();
            AniListAnime[] animes = new AniListAnime[count];
            int i = 0;

            while(usedIndices.Count < count)
            {
                int index = Random.Shared.Next(maxIndex);

                if (usedIndices.Add(index)) animes[i++] = page.Animes[index];
            }

            return animes;
        }

        public AniListAnime GetRandomAnime(AniListAnimePage page, int maxIndex = 50)
        {
            return GetRandomAnimes(page, 1, maxIndex)[0];
        }

        public (AniListAnime, AniListCharacter[]) GetRandomCharactersFromAnime(AniListAnime anime, int count, int maxIndex = 50)
        {
            if(anime.Characters.Count < count)
            {
                Console.WriteLine($"{_consoleTag} Anime has too few characters, using fallback...");
                AniListAnime fallback = AniListCache.GetRandomAnime() ?? throw new Exception($"{_consoleTag} No fallback anime available.");

                return GetRandomCharactersFromAnime(fallback, count, maxIndex);
            }

            maxIndex = Math.Min(maxIndex, anime.Characters.Count);
            HashSet<int> usedIndices = new();
            AniListCharacter[] characters = new AniListCharacter[count];
            int i = 0;

            while(usedIndices.Count < count)
            {
                int index = Random.Shared.Next(maxIndex);

                if (usedIndices.Add(index)) characters[i++] = anime.Characters[index];
            }

            return (anime, characters);
        }

        public (AniListAnime, AniListCharacter) GetRandomCharacterFromAnime(AniListAnime anime, int maxIndex = 50)
        {
            var (source, chars) = GetRandomCharactersFromAnime(anime, 1, maxIndex);

            return (source, chars[0]);
        }
    }
}
