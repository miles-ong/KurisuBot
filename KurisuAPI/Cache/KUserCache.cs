using KurisuCore.Models;
using Microsoft.Extensions.Caching.Memory;

namespace KurisuAPI.Cache
{
    public sealed class KUserCache
    {
        private readonly MemoryCache _cache = new(new MemoryCacheOptions());
        private readonly HashSet<KUser> _dirtyUsers = new();
        private readonly object _lock = new();
        private readonly int _cacheExpirationHours = 6;

        public bool TryGet(long id, out KUser? user)
        {
            return _cache.TryGetValue(id, out user);
        }

        public void Add(KUser user)
        {
            _cache.Set(user.Id, user, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(_cacheExpirationHours)
            });
        }

        public void MarkDirty(KUser user)
        {
            lock (_lock) _dirtyUsers.Add(user);
        }

        public HashSet<KUser> TakeDirty()
        {
            lock (_lock)
            {
                HashSet<KUser> snapshot = [.. _dirtyUsers];
                _dirtyUsers.Clear();

                return snapshot;
            }
        }
    }
}
