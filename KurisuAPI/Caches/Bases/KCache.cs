using System.Collections.Concurrent;
using KurisuCore.Models.Bases;

namespace KurisuAPI.Caches.Bases;

public abstract class KCache<TId, TEntity> : IKCache<TId, TEntity>
    where TId : IEquatable<TId>
    where TEntity : IKEntity<TId>
{
    protected readonly ConcurrentDictionary<TId, TEntity> Cache = new();

    public virtual bool TryGet(TId id, out TEntity? entity) => Cache.TryGetValue(id, out entity);
    public virtual void Add(TEntity entity) => Cache.TryAdd(entity.Id, entity);
    public virtual void Remove(TId id) => Cache.TryRemove(id, out _);
}

public abstract class KCache<TEntity> : KCache<int, TEntity>, IKCache<TEntity> where TEntity : IKEntity<int>;
