using KurisuAPI.Cache;
using KurisuAPI.Repositories;
using KurisuCore.Enums;
using KurisuCore.Models;
using KurisuCore.Result;
using Npgsql;

namespace KurisuAPI.Services;

public sealed class KCharacterService(
    KCharacterRepository repository, 
    KCharacterCache cache,
    KEntityLoader loader
    )
{
    private const string _consoleTag = "[KCharacterService]";

    public async Task<KCharacter?> GetByIdAsync(int id)
        => await loader.LoadCharacterAsync(id);

    public async Task<IEnumerable<KCharacter>> SearchAsync(string query, int limit = 10)
    {
        List<KCharacter> cached = cache.Search(query, limit).ToList();

        if (cached.Count >= limit) return cached;

        List<KCharacter> characters = await repository.SearchAsync(query, limit);
        List<KCharacter> results = new();

        foreach (KCharacter c in characters)
        {
            KCharacter? loaded = await loader.LoadCharacterAsync(c.Id);

            if (loaded is not null) results.Add(loaded);
        }

        return results;
    }

    public async Task<Result<KCharacter>> AddAsync(string name)
    {
        if (cache.TryGetByName(name, out _)) return Result<KCharacter>.Fail(KResult.IdenticalCharacterName);

        try
        {
            KCharacter character = await repository.InsertAsync(name);
            cache.Add(character);

            return Result<KCharacter>.Ok(character);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            Console.WriteLine($"{_consoleTag} Duplicate character '{name}': {ex.Message}");

            return Result<KCharacter>.Fail(KResult.CharacterAlreadyExists);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{_consoleTag} Failed to add character '{name}': {ex.Message}");

            return Result<KCharacter>.Fail(KResult.DatabaseFailure);
        }
    }

    public async Task<KResult> SetNameAsync(int id, string newName)
    {
        if (!cache.TryGet(id, out KCharacter? character)) return KResult.CannotFindCharacter;

        string oldName = character!.Name;
        KResult modelResult = character.SetName(newName);

        if (modelResult != KResult.Completed) return modelResult;

        try
        {
            await repository.UpdateAsync(character);
            cache.UpdateName(character);

            return KResult.Completed;
        }
        catch (KeyNotFoundException ex)
        {
            Console.WriteLine($"{_consoleTag} {ex.Message}");

            return KResult.CannotFindCharacter;
        }
        catch (Exception ex)
        {
            character.SetName(oldName);
            Console.WriteLine($"{_consoleTag} Failed to update character name for {character}: {ex.Message}");

            return KResult.DatabaseFailure;
        }
    }

    public async Task<KResult> DeleteAsync(int id)
    {
        if (!cache.TryGet(id, out KCharacter? character)) return KResult.CannotFindCharacter;

        try
        {
            await repository.DeleteAsync(id);
            cache.Remove(id);

            return KResult.Completed;
        }
        catch (KeyNotFoundException ex)
        {
            Console.WriteLine($"{_consoleTag} {ex.Message}");

            return KResult.CannotFindCharacter;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{_consoleTag} Failed to delete character {character}: {ex.Message}");

            return KResult.DatabaseFailure;
        }
    }
}
