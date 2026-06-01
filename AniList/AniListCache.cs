using Microsoft.Extensions.Caching.Memory;

namespace KurisuBot.AniList
{
    internal static class AniListCache
    {
        private static readonly MemoryCache _cache = new(new MemoryCacheOptions());
        private static readonly Dictionary<string, HashSet<string>> _keysByPrefix = new();
        private static readonly object _lock = new();

        private const int PageExpirationHours = 24;
        private const int AnimeExpirationHours = 24;
        private const int CharacterExpirationHours = 24;

        public static bool TryGetPage(string key, out AniListAnimePage? page)
        {
            return _cache.TryGetValue(key, out page);
        }

        public static bool TryGetAnime(string key, out AniListAnime? anime)
        {
            return _cache.TryGetValue(key, out anime);
        }

        public static bool TryGetCharacter(string key, out AniListCharacter? character)
        {
            return _cache.TryGetValue(key, out character);
        }

        public static void SetPage(string key, AniListAnimePage page)
        {
            _cache.Set(key, page, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(PageExpirationHours)
            });

            TrackKey("page", key);
        }

        public static void SetAnime(string key, AniListAnime anime)
        {
            _cache.Set(key, anime, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(AnimeExpirationHours)
            });

            TrackKey("anime", key);
        }

        public static void SetCharacter(string key, AniListCharacter character)
        {
            _cache.Set(key, character, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(CharacterExpirationHours)
            });

            TrackKey("character", key);
        }
        
        public static AniListAnimePage? GetRandomPage()
        {
            return GetRandom<AniListAnimePage>("page");
        }

        public static AniListAnime? GetRandomAnime()
        {
            return GetRandom<AniListAnime>("anime");
        }

        private static T? GetRandom<T>(string prefix) where T : class
        {
            lock (_lock)
            {
                if (!_keysByPrefix.TryGetValue(prefix, out var keys) || keys.Count == 0) return null;

                string[] candidates = keys.ToArray();
                string randomKey = candidates[Random.Shared.Next(candidates.Length)];

                if (_cache.TryGetValue(randomKey, out T? value)) return value;

                keys.Remove(randomKey);

                return GetRandom<T>(prefix);
            }
        }

        private static void TrackKey(string prefix, string key)
        {
            lock (_lock)
            {
                if (!_keysByPrefix.ContainsKey(prefix)) _keysByPrefix[prefix] = new();

                _keysByPrefix[prefix].Add(key);
            }
        }
    }
}
