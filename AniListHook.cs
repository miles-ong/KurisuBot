using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Text.Json;

namespace KurisuBot
{
    public class AniListData { }

    public class AniListAnimePage : AniListData
    {
        public string Genre { get; set; } = "all";
        public int PageNumber { get; set; }
        public string SortFilter { get; set; } = "POPULARITY_DESC";
        public List<AniListAnime> Animes { get; set; }
    }

    public class AniListAnime : AniListData
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public int Popularity { get; set; }
        public int AverageScore { get; set; }
        public string[] Genres { get; set; } = new string[3];
        public List<AniListCharacter> Characters { get; set; } = new();
    }

    public class AniListCharacter : AniListData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public int Favourites { get; set; }
    }

    public static class AniListHook
    {
        private static readonly int _animePageInfoExpirationTime = 24;
        private static readonly int _animeInfoExpirationTime = 24;
        private static readonly int _characterInfoExpirationTime = 24;

        private static readonly HttpClient _httpClient = new();
        private static readonly MemoryCache _cache = new(new MemoryCacheOptions());
        private static readonly Dictionary<string, HashSet<string>> _cacheKeysByPrefix = new();
        private static readonly object _lock = new();

        public static async Task InitializeAsync()
        {
            await GetAnimePageInfoAsync(1);
            Console.WriteLine("[AniListHook] Initialized");
        }

        public static async Task<AniListAnimePage> GetAnimePageInfoAsync(int pageNumber, string? genre = null, string sortFilter = "POPULARITY_DESC")
        {
            string query = string.IsNullOrEmpty(genre)
                ? @"
                    query ($page: Int) {
                        Page(page: $page, perPage: 50) {
                            media(type: ANIME, sort: SORT_PLACEHOLDER, isAdult: false, format_in: [TV, ONA, MOVIE]) {
                                id
                                title { romaji english native }
                                description(asHtml: false)
                                coverImage { large medium }
                                popularity
                                averageScore
                                genres
                                characters(page: 1, perPage: 50, sort: FAVOURITES_DESC) {
                                    edges {
                                        role
                                        node {
                                            id
                                            name { full native }
                                            image { large medium }
                                            favourites
                                        }
                                    }
                                }
                            }
                        }
                    }"
                : @"
                    query ($page: Int, $genre: String) {
                        Page(page: $page, perPage: 50) {
                            media(type: ANIME, sort: SORT_PLACEHOLDER, isAdult: false, genre_in: [$genre], format_in: [TV, ONA, MOVIE]) {
                                id
                                title { romaji english native }
                                description(asHtml: false)
                                coverImage { large medium }
                                popularity
                                averageScore
                                genres
                                characters(page: 1, perPage: 50, sort: FAVOURITES_DESC) {
                                    edges {
                                        role
                                        node {
                                            id
                                            name { full native }
                                            image { large medium }
                                            favourites
                                        }
                                    }
                                }
                            }
                        }
                    }";

            query = query.Replace("SORT_PLACEHOLDER", sortFilter);

            var variables = new Dictionary<string, object>
            {
                { "page", pageNumber }
            };

            if (!string.IsNullOrEmpty(genre))
            {
                variables["genre"] = genre;
            }

            string uniqueKey = $"G:{genre ?? "all"}_PN:{pageNumber}_SF:{sortFilter}";

            return (AniListAnimePage)await GetAniListDataAsync("anime_page_info", uniqueKey, query, variables);
        }

        public static AniListAnime[] GetRandomAnimes(AniListAnimePage animePage, int numberOfAnimes, int maxAnimeIndex = 50)
        {
            List<AniListAnime>? animes = animePage.Animes;

            if (animes == null || animes.Count < numberOfAnimes)
            {
                Console.WriteLine($"[AniListHook] '{animePage.Genre}_{animePage.PageNumber}_{animePage.SortFilter}' does not contain at least {numberOfAnimes} aniems. Requesting fallback...");
                AniListAnimePage fallbackAnimePage = (AniListAnimePage)GetRandomDataFromCache("anime_page_info");
                Console.WriteLine($"[AniListHook] Fallback found: '{fallbackAnimePage.Genre}_{fallbackAnimePage.PageNumber}_{fallbackAnimePage.SortFilter}'");

                return GetRandomAnimes(fallbackAnimePage, numberOfAnimes, maxAnimeIndex);
            }

            AniListAnime[] selectedAnimes = new AniListAnime[numberOfAnimes];
            maxAnimeIndex = Math.Min(maxAnimeIndex, animes.Count);
            HashSet<int> usedIndices = new();
            int i = 0;

            while(usedIndices.Count < numberOfAnimes)
            {
                int randomIndex = Random.Shared.Next(maxAnimeIndex);

                if(usedIndices.Add(randomIndex))
                {
                    selectedAnimes[i++] = animes[randomIndex];
                }
            }

            return selectedAnimes;
        }

        public static AniListAnime GetRandomAnime(AniListAnimePage animePage, int maxAnimeIndex = 50)
        {
            return GetRandomAnimes(animePage, 1, maxAnimeIndex)[0];
        }

        public static (AniListAnime, AniListCharacter[]) GetRandomCharactersFromAnime(AniListAnime anime, int numberOfCharacters, int maxCharacterIndex = 50)
        {
            List<AniListCharacter> characters = anime.Characters;
            AniListCharacter[] selectedCharacters = new AniListCharacter[numberOfCharacters];
            maxCharacterIndex = Math.Min(maxCharacterIndex, characters.Count);

            if(characters.Count < numberOfCharacters)
            {
                AniListAnime fallbackAnime = (AniListAnime)GetRandomDataFromCache("anime_info");

                return GetRandomCharactersFromAnime(fallbackAnime, numberOfCharacters, maxCharacterIndex);
            }

            HashSet<int> usedIndices = new();
            int i = 0;

            while(usedIndices.Count < numberOfCharacters)
            {
                int randomIndex = Random.Shared.Next(maxCharacterIndex);

                if(usedIndices.Add(randomIndex))
                {
                    selectedCharacters[i++] = characters[randomIndex];
                }
            }

            return (anime, selectedCharacters);
        }

        public static (AniListAnime, AniListCharacter) GetRandomCharacterFromAnime(AniListAnime anime, int characterPageRange = 50)
        {
            var(source, characterArray) = GetRandomCharactersFromAnime(anime, 1, characterPageRange);

            return (source, characterArray.First());
        }
        
        private static async Task<AniListData> GetAniListDataAsync(string dataPrefix, string uniqueKey, string query, Dictionary<string, object> variables)
        {
            string key = $"{dataPrefix.ToLowerInvariant()}_{uniqueKey.Trim().ToLowerInvariant()}";

            if(_cache.TryGetValue(key, out AniListData cachedData))
            {
                return cachedData;
            }

            var requestBody = new { query, variables = variables ?? new Dictionary<string, object>() };
            StringContent content = new(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync("https://graphql.anilist.co", content);
                string jsonString = await response.Content.ReadAsStringAsync();
                using JsonDocument jsonDoc = JsonDocument.Parse(jsonString);

                switch (dataPrefix)
                {
                    case "anime_page_info":
                        JsonElement animePageInfo = jsonDoc.RootElement.GetProperty("data").GetProperty("Page").GetProperty("media");

                        return CacheAnimePageInfo(key, animePageInfo);

                    case "anime_info":
                        JsonElement animeInfo = jsonDoc.RootElement.GetProperty("data").GetProperty("Media");

                        return CacheAnimeInfo(key, animeInfo);

                    case "character_info":
                        JsonElement characterInfo = jsonDoc.RootElement.GetProperty("data").GetProperty("Character");

                        return CacheCharacterInfo(key, characterInfo);
                }

                return GetRandomDataFromCache(dataPrefix);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AniListCache] Failed to retrieve AniList data: {ex.Message}");
                return GetRandomDataFromCache(dataPrefix);
            }
        }

        private static AniListData GetRandomDataFromCache(string prefix)
        {
            if(!_cacheKeysByPrefix.TryGetValue(prefix, out var keys) || keys.Count == 0)
            {
                throw new Exception($"No cached keys found for prefix: {prefix}");
            }

            string[] candidates = keys.ToArray();
            string randomKey = candidates[Random.Shared.Next(candidates.Length)];

            if(_cache.TryGetValue(randomKey, out AniListData fallback))
            {
                return fallback;
            }

            lock(_lock)
            {
                keys.Remove(randomKey);
            }

            return GetRandomDataFromCache(prefix);
        }

        private static AniListAnimePage CacheAnimePageInfo(string key, JsonElement info)
        {
            if(_cache.TryGetValue(key, out AniListAnimePage cached))
            {
                return cached;
            }

            string[] keyParts = key.Split('_', StringSplitOptions.RemoveEmptyEntries);
            string genre = "all";
            int pageNumber = 1;
            string sortFilter = "POPULARITY_DESC";

            foreach(var part in keyParts)
            {
                string[] kv = part.Split(':', 2);

                if(kv.Length != 2)
                {
                    continue;
                }

                switch(kv[0].ToUpperInvariant())
                {
                    case "G":
                        genre = kv[1];
                        break;
                    case "PN":
                        pageNumber = int.Parse(kv[1]);
                        break;
                    case "SF":
                        sortFilter = kv[1];
                        break;
                }
            }

            List<AniListAnime> animeList = new();

            foreach(JsonElement anime in info.EnumerateArray())
            {
                string animeKey = $"anime_info_{anime.GetProperty("id").GetInt32()}";
                animeList.Add(CacheAnimeInfo(animeKey, anime));
            }

            AniListAnimePage animePage = new()
            {
                Genre = genre,
                PageNumber = pageNumber,
                SortFilter = sortFilter,
                Animes = animeList
            };

            _cache.Set(key, animePage, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(_animePageInfoExpirationTime)
            });

            TrackKey("anime_page_info", key);

            Console.WriteLine($"[AniListHook] Cached page '{genre}_{pageNumber}_{sortFilter}' to {key}");
            return animePage;
        }

        private static AniListAnime CacheAnimeInfo(string key, JsonElement info)
        {
            if(_cache.TryGetValue(key, out AniListAnime cached))
            {
                return cached;
            }

            string[] genreArray = info.TryGetProperty("genres", out var genres)
                ? genres.EnumerateArray().Take(3).Select(g => g.GetString() ?? "").ToArray()
                : new string[3];

            List<AniListCharacter> characters = new();

            if(info.TryGetProperty("characters", out var chars) && chars.TryGetProperty("edges", out var edges))
            {
                foreach(JsonElement character in edges.EnumerateArray())
                {
                    JsonElement node = character.GetProperty("node");
                    string cKey = $"character_info_{node.GetProperty("id").GetInt32()}";
                    AniListCharacter? cachedChar = CacheCharacterInfo(cKey, node);

                    if(cachedChar != null)
                    {
                        characters.Add(cachedChar);
                    }
                }
            }

            AniListAnime anime = new()
            {
                Id = info.GetProperty("id").GetInt32(),
                Title = GetAnimeTitle(info),
                Description = GetAnimeDescription(info),
                ImageUrl = GetAnimeImageUrl(info),
                Popularity = info.TryGetProperty("popularity", out var pop) && pop.ValueKind == JsonValueKind.Number ? pop.GetInt32() : 0,
                AverageScore = info.TryGetProperty("averageScore", out var score) && score.ValueKind == JsonValueKind.Number ? score.GetInt32() : 0,
                Genres = genreArray,
                Characters = characters
            };

            _cache.Set(key, anime, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(_animeInfoExpirationTime)
            });

            TrackKey("anime_info", key);

            Console.WriteLine($"[AniListHook] Cached anime '{key}' to ID: {anime.Id}");
            return anime;
        }

        private static AniListCharacter CacheCharacterInfo(string key, JsonElement info)
        {
            if(_cache.TryGetValue(key, out AniListCharacter cached))
            {
                return cached;
            }

            AniListCharacter character = new()
            {
                Id = info.GetProperty("id").GetInt32(),
                Name = GetCharacterName(info),
                ImageUrl = GetCharacterImageUrl(info),
                Favourites = info.TryGetProperty("favourites", out var fav) && fav.ValueKind == JsonValueKind.Number ? fav.GetInt32() : 0
            };

            _cache.Set(key, character, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(_characterInfoExpirationTime)
            });

            TrackKey("character_info", key);

            Console.WriteLine($"[AniListHook] Cached character '{key}' to ID: {character.Id}");
            return character;
        }

        private static void TrackKey(string prefix, string key)
        {
            lock(_lock)
            {
                if(!_cacheKeysByPrefix.ContainsKey(prefix))
                {
                    _cacheKeysByPrefix[prefix] = new();
                }

                _cacheKeysByPrefix[prefix].Add(key);
            }
        }

        private static string GetAnimeTitle(JsonElement anime)
        {
            if(anime.TryGetProperty("title", out var titleElement))
            {
                if(titleElement.TryGetProperty("english", out var englishElement))
                {
                    string? english = englishElement.GetString();

                    if(!string.IsNullOrEmpty(english))
                    {
                        return english;
                    }
                }

                if(titleElement.TryGetProperty("romaji", out var romajiElement))
                {
                    string? romaji = romajiElement.GetString();

                    if(!string.IsNullOrEmpty(romaji))
                    {
                        return romaji;
                    }
                }

                if(titleElement.TryGetProperty("native", out var nativeElement))
                {
                    string? native = nativeElement.GetString();

                    if(!string.IsNullOrEmpty(native))
                    {
                        return native;
                    }
                }
            }

            return "Unknown Anime";
        }

        private static string? GetAnimeImageUrl(JsonElement anime)
        {
            if (anime.TryGetProperty("coverImage", out var coverImageElement))
            {
                if (coverImageElement.TryGetProperty("large", out var largeElement))
                {
                    return largeElement.GetString();
                }

                if (coverImageElement.TryGetProperty("medium", out var mediumElement))
                {
                    return mediumElement.GetString();
                }
            }

            return null;
        }

        private static string GetAnimeDescription(JsonElement anime)
        {
            if (anime.TryGetProperty("description", out var descriptionElement))
            {
                string? description = descriptionElement.GetString();

                if (!string.IsNullOrEmpty(description))
                {
                    return System.Text.RegularExpressions.Regex.Replace(description, "<.*?>", string.Empty);
                }
            }

            return "Description not found.";
        }

        private static string GetCharacterName(JsonElement character)
        {
            if (character.TryGetProperty("name", out var nameElement))
            {
                if (nameElement.TryGetProperty("full", out var fullElement))
                {
                    string? full = fullElement.GetString();

                    if (!string.IsNullOrEmpty(full))
                    {
                        return full;
                    }
                }

                if (nameElement.TryGetProperty("native", out var nativeElement))
                {
                    string? native = nativeElement.GetString();

                    if (!string.IsNullOrEmpty(native))
                    {
                        return native;
                    }
                }

                if (nameElement.TryGetProperty("alternative", out var altElement) &&
                    altElement.ValueKind == JsonValueKind.Array && altElement.GetArrayLength() > 0)
                {
                    string? alternative = altElement[0].GetString();

                    if (!string.IsNullOrEmpty(alternative))
                    {
                        return alternative;
                    }
                }
            }

            return "Unknown Character";
        }

        private static string? GetCharacterImageUrl(JsonElement character)
        {
            if (character.TryGetProperty("image", out var imageElement))
            {
                if (imageElement.TryGetProperty("large", out var largeElement))
                {
                    return largeElement.GetString();
                }

                if (imageElement.TryGetProperty("medium", out var mediumElement))
                {
                    return mediumElement.GetString();
                }
            }

            return null;
        }
    }
}
