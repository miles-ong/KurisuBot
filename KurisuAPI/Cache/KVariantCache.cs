using KurisuCore.Models;
using System.Collections.Concurrent;

namespace KurisuAPI.Cache
{
    public sealed class KVariantCache
    {
        private readonly ConcurrentDictionary<int, KVariant> _cache = new();

        public bool TryGet(int id, out KVariant? variant)
        {
            return _cache.TryGetValue(id, out variant);
        }

        public void Add(KVariant variant)
        {
            _cache.TryAdd(variant.Id, variant);
        }

        public void Remove(int id)
        {
            _cache.TryRemove(id, out _);
        }
    }
}
