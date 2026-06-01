using KurisuBot.Core.Models;
using System.Collections.Concurrent;

namespace KurisuBot.Core.Infrastructure.Cache
{
    internal static class KVariantCache
    {
        private static readonly ConcurrentDictionary<int, KVariant> _cache = new();

        public static IEnumerable<KVariant> All => _cache.Values;

        public static bool TryGet(int id, out KVariant? variant)
        {
            return _cache.TryGetValue(id, out variant);
        }

        public static void Add(KVariant variant)
        {
            _cache.TryAdd(variant.Id, variant);
        }

        public static void Remove(int id)
        {
            _cache.TryRemove(id, out _);
        }
    }
}
