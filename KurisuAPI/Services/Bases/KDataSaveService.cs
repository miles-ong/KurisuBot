using KurisuAPI.Caches.Bases;
using KurisuAPI.Factories;
using KurisuAPI.Repositories.Bases;
using KurisuCore.Models.Bases;

namespace KurisuAPI.Services.Bases;

public abstract class KDataSaveService<TId, TEntity, TData>(
    KDataBulkRepository<TId, TData> repo,
    KSaveCache<TId, TEntity> cache,
    KEntityFactory factory)
    : KDataService<TId, TEntity, TData>(repo, cache, factory)
    where TId : IEquatable<TId>
    where TEntity : KEntity<TId, TData>
    where TData : IKData<TId>
{
    protected abstract int SaveLoopIntervalMinutes { get; }

    private CancellationTokenSource _cts = new();

    public void MarkDirty(TEntity entity) => cache.MarkDirty(entity);

    public async Task StartSaveLoopAsync()
    {
        _cts = new CancellationTokenSource();
        Console.WriteLine($"{ConsoleTag} Started save loop");

        while (!_cts.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(SaveLoopIntervalMinutes), _cts.Token);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            await FlushAsync(_cts.Token);
        }
    }

    public async Task StopSaveLoopAsync()
    {
        await _cts.CancelAsync();
        await FlushAsync(CancellationToken.None);
        Console.WriteLine($"{ConsoleTag} Stopped save loop");
    }

    private async Task FlushAsync(CancellationToken token)
    {
        await SaveDirtyEntitiesAsync(token);
        await DeletePendingEntitesAsync(token);
    }

    private async Task SaveDirtyEntitiesAsync(CancellationToken token)
    {
        var dirty = cache.TakeDirty();

        if (dirty.Count == 0) return;

        try
        {
            await repo.BulkUpsertAsync(dirty.Select(e => e.ToData()), token);
            Console.WriteLine($"{ConsoleTag} Saved {dirty.Count} {ModelName}(s)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ConsoleTag} Failed to save {dirty.Count} {ModelName}(s): {ex.Message}");

            foreach (var e in dirty) cache.MarkDirty(e);
        }
    }

    private async Task DeletePendingEntitesAsync(CancellationToken token)
    {
        var pending = cache.TakeDelete();

        if (pending.Count == 0) return;

        try
        {
            await repo.BulkDeleteAsync(pending, token);
            Console.WriteLine($"{ConsoleTag} Deleted {pending.Count} {ModelName}(s)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ConsoleTag} Failed to delete {pending.Count} {ModelName}(s): {ex.Message}");
            cache.RequeueDelete(pending);
        }
    }
}

public abstract class KDataSaveService<TEntity, TData>(
    KDataBulkRepository<long, TData> repo,
    KSaveCache<TEntity> cache,
    KEntityFactory factory)
    : KDataSaveService<long, TEntity, TData>(repo, cache, factory)
    where TEntity : KEntity<long, TData>
    where TData : IKData<long>;