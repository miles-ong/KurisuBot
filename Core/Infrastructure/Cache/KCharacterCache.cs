using FuzzySharp;
using KurisuBot.Core.Models;
using System.Collections.Concurrent;

namespace KurisuBot.Core.Infrastructure.Cache
{
    internal static class KCharacterCache
    {
        private static readonly ConcurrentDictionary<int, KCharacter> _cache = new();
        private static readonly ConcurrentDictionary<string, KCharacter> _nameIndex =
            new(StringComparer.OrdinalIgnoreCase);
        public static IEnumerable<KCharacter> All => _cache.Values;

        public static bool TryGet(int id, out KCharacter? character)
        {
            return _cache.TryGetValue(id, out character);
        }

        public static bool TryGetByName(string name, out KCharacter? character)
        {
            return _nameIndex.TryGetValue(name, out character);
        }

        public static void Add(KCharacter character)
        {
            _cache.TryAdd(character.Id, character);
            _nameIndex.TryAdd(character.Name, character);
        }

        public static void Remove(int id)
        {
            if(_cache.TryRemove(id, out var character))
            {
                _nameIndex.TryRemove(character.Name, out _);
            }
        }

        public static void UpdateName(KCharacter character)
        {
            var stale = _nameIndex
                .Where(x => x.Value.Id == character.Id)
                .Select(x => x.Key)
                .ToList();

            foreach(var key in stale)
            {
                _nameIndex.TryRemove(key, out _);
            }

            _nameIndex.TryAdd(character.Name, character);
        }

        public static IEnumerable<KCharacter> Search(string query, int limit = 10)
        {
            if (TryGetByName(query, out KCharacter? exact)) return [exact!];

            var prefix = _nameIndex
                .Where(x => x.Key.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Value)
                .Take(limit)
                .ToList();

            if (prefix.Count > 0) return prefix;

            return _nameIndex.Values
                .Select(c => new
                {
                    Character = c,
                    Score = Fuzz.Ratio(query, c.Name)
                })
                .Where(x => x.Score > 70)
                .OrderByDescending(x => x.Score)
                .Take(limit)
                .Select(x => x.Character);
        }
    }
}
