using KurisuCore.Models.Bases;
using Microsoft.Extensions.Caching.Memory;

namespace KurisuAPI.Caches.Bases;

public abstract class KSaveCache<TId, TEntity> : IKCache<TId, TEntity>
    where TId : IEquatable<TId>
    where TEntity : IKEntity<TId>
{
    protected abstract int CacheExpirationHours { get; }

    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly HashSet<TEntity> _dirty = [];
    private readonly HashSet<TId> _delete = [];
    private readonly Lock _dirtyLock = new();
    private readonly Lock _deleteLock = new();

    public bool TryGet(TId id, out TEntity? entity) => _cache.TryGetValue(id, out entity);

    public void Add(TEntity entity)
    {
        _cache.Set(entity.Id, entity, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(CacheExpirationHours)
        });

        lock (_deleteLock) _delete.Remove(entity.Id);
    }

    public void Remove(TId id)
    {
        if (!TryGet(id, out var entity)) return;

        _cache.Remove(id);
        
        lock (_dirtyLock) _dirty.Remove(entity!);
        lock (_deleteLock) _delete.Add(id);
    }

    public void MarkDirty(TEntity entity)
    {
        lock (_deleteLock)
        {
            if (_delete.Contains(entity.Id)) return;
        }
        
        lock (_dirtyLock) _dirty.Add(entity);
    }

    public HashSet<TEntity> TakeDirty()
    {
        lock (_dirtyLock)
        {
            HashSet<TEntity> snapshot = [.. _dirty];
            _dirty.Clear();

            return snapshot;
        }
    }

    public HashSet<TId> TakeDelete()
    {
        lock (_deleteLock)
        {
            HashSet<TId> snapshot = [.._delete];
            _delete.Clear();

            return snapshot;
        }
    }

    public void RequeueDelete(IEnumerable<TId> ids)
    {
        lock (_deleteLock)
        {
            foreach (var id in ids) _delete.Add(id);
        }
    }
}

public abstract class KSaveCache<TEntity> : KSaveCache<long, TEntity> where TEntity : IKEntity<long>;