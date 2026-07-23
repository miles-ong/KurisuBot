using System.Runtime.InteropServices;
using KurisuAPI.Caches.Bases;
using KurisuAPI.Factories;
using KurisuAPI.Repositories.Bases;
using KurisuCore.Models.Bases;

namespace KurisuAPI.Services.Bases;

public abstract class KDataSearchService<TId, TEntity, TData>(
    KDataSearchRepository<TId, TData> repo,
    KIndexedCache<TId, TEntity> cache,
    KEntityFactory factory)
    : KDataService<TId, TEntity, TData>(repo, cache, factory)
    where TId : IEquatable<TId>
    where TEntity : KEntity<TId, TData>
    where TData : IKData<TId>
{
    public async Task<IEnumerable<TEntity>> SearchAsync(string query, int limit = 10)
    {
        var cached = cache.Search(query, limit).ToList();

        if (cached.Count >= limit) return cached;

        var data = await repo.SearchAsync(query, limit);
        var results = new List<TEntity>();

        foreach (var d in data)
        {
            var loaded = await Factory.LoadAsync<TId, TEntity, TData>(d.Id);

            if (loaded is not null) results.Add(loaded);
        }

        return results;
    }
}

public abstract class KDataSearchService<TEntity, TData>(
    KDataSearchRepository<int, TData> repo,
    KIndexedCache<TEntity> cache,
    KEntityFactory factory)
    : KDataSearchService<int, TEntity, TData>(repo, cache, factory)
    where TEntity : KEntity<TData>
    where TData : IKData;
