using KurisuAPI.Cache;
using KurisuAPI.Repositories;
using KurisuCore.Enums;
using KurisuCore.Models;
using KurisuCore.Result;
using Npgsql;

namespace KurisuAPI.Services;

public sealed class KAnimeService
{
    private readonly KAnimeRepository _repository;
    private const string _consoleTag = "[KAnimeService]";

    public KAnimeService(string connectionString)
    {
        _repository = new KAnimeRepository(connectionString);
    }

    public async Task InitializeAsync()
    {
        List<KAnime> all = await _repository.GetAllAsync();

        foreach(KAnime anime in all)
        {
            KAnimeCache.Add(anime);
        }

        Console.WriteLine($"{_consoleTag} Initialized {all.Count} animes");
    }

    public bool TryGet(int id, out KAnime? anime)
    {
        return KAnimeCache.TryGet(id, out anime);
    }

    public IEnumerable<KAnime> Search(string query, int limit = 10)
    {
        return KAnimeCache.Search(query, limit);
    }

    public async Task<Result<KAnime>> AddAsync(string title, string? description = null)
    {
        if (KAnimeCache.TryGetByTitle(title, out _)) return Result<KAnime>.Fail(KResult.AnimeAlreadyExists);

        try
        {
            KAnime anime = await _repository.InsertAsync(title, description);
            KAnimeCache.Add(anime);

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
        if (!KAnimeCache.TryGet(id, out KAnime? anime)) return KResult.CannotFindAnime;

        string oldTitle = anime!.Title;
        KResult modelResult = anime.SetTitle(newTitle);

        if (modelResult != KResult.Completed) return modelResult;

        try
        {
            await _repository.UpdateAsync(anime);
            KAnimeCache.UpdateTitle(anime);

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
        if (!KAnimeCache.TryGet(id, out KAnime? anime)) return KResult.CannotFindAnime;

        string oldDescription = anime!.Description;
        KResult modelResult = anime.SetDescription(newDescription);

        if (modelResult != KResult.Completed) return modelResult;

        try
        {
            await _repository.UpdateAsync(anime);

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
        if (!KAnimeCache.TryGet(id, out KAnime? anime)) return KResult.CannotFindAnime;

        try
        {
            await _repository.DeleteAsync(id);
            KAnimeCache.Remove(id);

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
