using FuzzySharp;
using KurisuCore.Models;
using System.Collections.Concurrent;

namespace KurisuAPI.Cache
{
    internal static class KAnimeCache
    {
        private static readonly ConcurrentDictionary<int, KAnime> _cache = new();
        private static readonly ConcurrentDictionary<string, KAnime> _titleIndex =
            new(StringComparer.OrdinalIgnoreCase);
        public static IEnumerable<KAnime> All => _cache.Values;

        public static bool TryGet(int id, out KAnime? anime)
        {
            return _cache.TryGetValue(id, out anime);
        }

        public static bool TryGetByTitle(string title, out KAnime? anime)
        {
            return _titleIndex.TryGetValue(title, out anime);
        }

        public static void Add(KAnime anime)
        {
            _cache.TryAdd(anime.Id, anime);
            _titleIndex.TryAdd(anime.Title, anime);
        }

        public static void Remove(int id)
        {
            if(_cache.TryRemove(id, out var anime))
            {
                _titleIndex.TryRemove(anime.Title, out _);
            }
        }

        public static void UpdateTitle(KAnime anime)
        {
            var stale = _titleIndex
                .Where(x => x.Value.Id == anime.Id)
                .Select(x => x.Key)
                .ToList();

            foreach(var key in stale)
            {
                _titleIndex.TryRemove(key, out _);
            }

            _titleIndex.TryAdd(anime.Title, anime);
        }

        public static IEnumerable<KAnime> Search(string query, int limit = 10)
        {
            if (TryGetByTitle(query, out KAnime? exact)) return [exact!];

            var prefix = _titleIndex
                .Where(x => x.Key.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Value)
                .Take(limit)
                .ToList();

            if (prefix.Count > 0) return prefix;

            return _titleIndex.Values
                .Select(a => new
                {
                    Anime = a,
                    Score = Fuzz.Ratio(query, a.Title)
                })
                .Where(x => x.Score > 70)
                .OrderByDescending(x => x.Score)
                .Take(limit)
                .Select(x => x.Anime);
        }
    }
}
