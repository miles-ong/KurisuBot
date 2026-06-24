using KurisuAPI.Cache;
using KurisuAPI.Repositories;
using KurisuCore.Enums;
using KurisuCore.Models;
using KurisuCore.Result;
using Npgsql;

namespace KurisuAPI.Services;

public sealed class KAnimeService(
    KAnimeRepository repository, 
    KAnimeCache cache,
    KEntityLoader loader
    )
{
    public async Task<KAnime?> GetByIdAsync(int id)
        => await loader.LoadAnimeAsync(id);

    public async Task<IEnumerable<KAnime>> SearchAsync(string query, int limit = 10)
    {
        List<KAnime> cached = cache.Search(query, limit).ToList();

        if (cached.Count >= limit) return cached;

        List<KAnime> animes = await repository.SearchAsync(query, limit);
        List<KAnime> results = new();

        foreach(KAnime a in animes)
        {
            KAnime? loaded = await loader.LoadAnimeAsync(a.Id);

            if (loaded is not null) results.Add(loaded);
        }

        return results;
    }

    public async Task<Result<KAnime>> AddAsync(string title, string? description = null)
    {
        if (cache.TryGetByTitle(title, out _)) return Result<KAnime>.Fail(KResult.AnimeAlreadyExists);

        try
        {
            KAnime anime = await repository.InsertAsync(title, description);
            cache.Add(anime);

            return Result<KAnime>.Ok(anime);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Result<KAnime>.Fail(KResult.AnimeAlreadyExists);
        }
        catch (Exception ex)
        {
            return Result<KAnime>.Fail(KResult.DatabaseFailure);
        }
    }

    public async Task<KResult> SetTitleAsync(int id, string newTitle)
    {
        if (!cache.TryGet(id, out KAnime? anime)) return KResult.CannotFindAnime;

        string oldTitle = anime!.Title;
        KResult modelResult = anime.SetTitle(newTitle);

        if (modelResult != KResult.Completed) return modelResult;

        try
        {
            await repository.UpdateAsync(anime);
            cache.UpdateTitle(anime);

            return KResult.Completed;
        }
        catch (KeyNotFoundException)
        {
            return KResult.CannotFindAnime;
        }
        catch (Exception)
        {
            anime.SetTitle(oldTitle);

            return KResult.DatabaseFailure;
        }
    }

    public async Task<KResult> SetDescriptionAsync(int id, string newDescription)
    {
        if (!cache.TryGet(id, out KAnime? anime)) return KResult.CannotFindAnime;

        string oldDescription = anime!.Description;
        KResult modelResult = anime.SetDescription(newDescription);

        if (modelResult != KResult.Completed) return modelResult;

        try
        {
            await repository.UpdateAsync(anime);

            return KResult.Completed;
        }
        catch (KeyNotFoundException ex)
        {
            return KResult.CannotFindAnime;
        }
        catch (Exception)
        {
            anime.SetDescription(oldDescription);

            return KResult.DatabaseFailure;
        }
    }

    public async Task<KResult> DeleteAsync(int id)
    {
        if (!cache.TryGet(id, out KAnime? anime)) return KResult.CannotFindAnime;

        try
        {
            await repository.DeleteAsync(id);
            cache.Remove(id);

            return KResult.Completed;
        }
        catch (KeyNotFoundException)
        {
            return KResult.CannotFindAnime;
        }
        catch (Exception)
        {
            return KResult.DatabaseFailure;
        }
    }
}
