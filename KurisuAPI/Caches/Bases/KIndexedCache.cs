using FuzzySharp;
using System.Collections.Concurrent;
using KurisuCore.Models.Bases;

namespace KurisuAPI.Caches.Bases;

public abstract class KIndexedCache<TId, TEntity> : KCache<TId, TEntity>
    where TId : IEquatable<TId>
    where TEntity : IKEntity<TId>
{
    private readonly ConcurrentDictionary<string, TEntity> _index = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<TEntity> Search(string query, int limit = 10)
    {
        if (TryGetByKey(query, out var exact)) return [exact!];

        var prefix = _index
            .Where(x => x.Key.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Value)
            .Take(limit)
            .ToList();

        if (prefix.Count > 0) return prefix;

        return _index.Values
            .Select(e => new { Entity = e, Score = Fuzz.Ratio(query, GetKey(e)) })
            .Where(x => x.Score > 70)
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .Select(x => x.Entity);
    }

    public override void Add(TEntity entity)
    {
        base.Add(entity);
        _index.TryAdd(GetKey(entity), entity);
    }

    public override void Remove(TId id)
    {
        if (Cache.TryRemove(id, out var entity)) _index.TryRemove(GetKey(entity), out _);
    }
    
    protected bool TryGetByKey(string key, out TEntity? entity) => _index.TryGetValue(key, out entity);
        
    protected abstract string GetKey(TEntity entity);

    protected void UpdateKey(TEntity entity, string oldKey)
    {
        _index.TryRemove(oldKey, out _);
        _index.TryAdd(GetKey(entity), entity);
    }
}

public abstract class KIndexedCache<TEntity> : KIndexedCache<int, TEntity> where TEntity : IKEntity<int>;