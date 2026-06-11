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
    KCharacterAnimeLinkService linkService
    )
{
    private const string _consoleTag = "[KAnimeService]";

    public async Task<KAnime?> GetByIdAsync(int id)
    {
        if (cache.TryGet(id, out KAnime? cached)) return cached;

        KAnime? anime = await repository.GetByIdAsync(id);

        if (anime is not null)
        {
            await linkService.LoadLinksAsync(anime);
            cache.Add(anime);
        }

        return anime;
    }

    public async Task<IEnumerable<KAnime>> SearchAsync(string query, int limit = 10)
    {
        List<KAnime> cached = cache.Search(query, limit).ToList();

        if (cached.Count >= limit) return cached;

        List<KAnime> animes = await repository.SearchAsync(query, limit);

        foreach(KAnime anime in animes)
        {
            cache.Add(anime);
        }

        return animes;
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
            Console.WriteLine($"{_consoleTag} Duplicate anime '{title}': {ex.Message}");

            return Result<KAnime>.Fail(KResult.AnimeAlreadyExists);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{_consoleTag} Failed to add anime '{title}': {ex.Message}");

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
        catch (KeyNotFoundException ex)
        {
            Console.WriteLine($"{_consoleTag} {ex.Message}");

            return KResult.CannotFindAnime;
        }
        catch (Exception ex)
        {
            anime.SetTitle(oldTitle);
            Console.WriteLine($"{_consoleTag} Failed to update anime title for {anime}: {ex.Message}");

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
            Console.WriteLine($"{_consoleTag} {ex.Message}");

            return KResult.CannotFindAnime;
        }
        catch (Exception ex)
        {
            anime.SetDescription(oldDescription);
            Console.WriteLine($"{_consoleTag} Failed to update anime description for {anime}: {ex.Message}");

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
        catch (KeyNotFoundException ex)
        {
            Console.WriteLine($"{_consoleTag} {ex.Message}");

            return KResult.CannotFindAnime;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{_consoleTag} Failed to delete anime {anime}: {ex.Message}");

            return KResult.DatabaseFailure;
        }
    }
}
