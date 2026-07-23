using KurisuCore.Models.Bases;

namespace KurisuAPI.Caches.Bases;

public interface IKCache<in TId, TEntity>
    where TId : IEquatable<TId>
    where TEntity : IKEntity<TId>
{
    public bool TryGet(TId id, out TEntity? entity);
    public void Add(TEntity entity);
    public void Remove(TId id);
}

public interface IKCache<TEntity> : IKCache<int, TEntity> where TEntity : IKEntity<int>;