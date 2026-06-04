using KurisuCore.Models;
using Microsoft.Extensions.Caching.Memory;

namespace KurisuAPI.Cache
{
    internal static class KUserCache
    {
        private static readonly MemoryCache _cache = new(new MemoryCacheOptions());
        private static readonly HashSet<KUser> _dirtyUsers = new();
        private static readonly object _lock = new();
        private static readonly int _cacheExpirationHours = 6;

        public static bool TryGet(long id, out KUser? user)
        {
            return _cache.TryGetValue(id, out user);
        }

        public static void Add(KUser user)
        {
            _cache.Set(user.Id, user, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(_cacheExpirationHours)
            });
        }

        public static void MarkDirty(KUser user)
        {
            lock (_lock) _dirtyUsers.Add(user);
        }

        public static HashSet<KUser> TakeDirty()
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
