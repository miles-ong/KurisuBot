using KurisuAPI.Cache;
using KurisuAPI.Records;
using KurisuAPI.Repositories;
using KurisuCore.Enums;
using KurisuCore.Models;
using KurisuCore.Result;
using Npgsql;

namespace KurisuAPI.Services;

public sealed class KVariantService(
    KVariantRepository repository, 
    KVariantCache cache,
    KEntityLoader loader
    )
{
    private const string _consoleTag = "[KVariantService]";

    public async Task<KVariant?> GetByIdAsync(int id)
    {
        if (cache.TryGet(id, out KVariant? cached)) return cached;

        KVariantRow? row = await repository.GetByIdAsync(id);

        if (row is null) return null;

        KCharacter? character = await loader.LoadCharacterAsync(row.CharacterId);
        KAnime? anime = await loader.LoadAnimeAsync(row.AnimeId);

        if (character is null || anime is null) return null;

        KVariant variant = new(row.Id, character, anime, row.Rarity, row.Title);
        cache.Add(variant);
        character.AddVariant(variant);

        return variant;
    }

    public async Task<Result<KVariant>> AddAsync(int cid, int aid, KRarity rarity, string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return Result<KVariant>.Fail(KResult.InvalidVariantTitle);

        KCharacter? character = await loader.LoadCharacterAsync(cid);
        KAnime? anime = await loader.LoadAnimeAsync(aid);

        if (character is null) return Result<KVariant>.Fail(KResult.CannotFindCharacter);
        if (anime is null) return Result<KVariant>.Fail(KResult.CannotFindAnime);
        if (!character.ContainsAnime(anime!)) return Result<KVariant>.Fail(KResult.CannotFindCharacterAnimeLink);

        try
        {
            int id = await repository.InsertAsync(cid, aid, rarity, title);
            KVariant variant = new KVariant(id, character, anime, rarity, title);
            character.AddVariant(variant);
            cache.Add(variant);

            return Result<KVariant>.Ok(variant);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            Console.WriteLine($"{_consoleTag} Duplicate variant (CID: {cid}) '{title}': {ex.Message}");

            return Result<KVariant>.Fail(KResult.VariantAlreadyExists);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{_consoleTag} Failed to add variant (CID: {cid}) '{title}': {ex.Message}");

            return Result<KVariant>.Fail(KResult.DatabaseFailure);
        }
    }

    public async Task<KResult> SetAnimeAsync(int id, KAnime newAnime)
    {
        if (!cache.TryGet(id, out KVariant? variant)) return KResult.CannotFindVariant;

        KAnime oldAnime = variant!.Anime;
        KResult modelResult = variant.SetAnime(newAnime);

        if (modelResult != KResult.Completed) return modelResult;

        try
        {
            await repository.UpdateAsync(variant);

            return KResult.Completed;
        }
        catch (Exception ex)
        {
            variant.SetAnime(oldAnime);
            Console.WriteLine($"{_consoleTag} Failed to update variant anime for {variant}: {ex.Message}");

            return KResult.DatabaseFailure;
        }
    }

    public async Task<KResult> SetRarityAsync(int id, KRarity newRarity)
    {
        if (!cache.TryGet(id, out KVariant? variant)) return KResult.CannotFindVariant;

        KRarity oldRarity = variant!.Rarity;
        KResult modelResult = variant.SetRarity(newRarity);

        if (modelResult != KResult.Completed) return modelResult;

        try
        {
            await repository.UpdateAsync(variant);

            return KResult.Completed;
        }
        catch (Exception ex)
        {
            variant.SetRarity(oldRarity);
            Console.WriteLine($"{_consoleTag} Failed to update variant rarity for {variant}: {ex.Message}");

            return KResult.DatabaseFailure;
        }
    }

    public async Task<KResult> SetTitleAsync(int id, string newTitle)
    {
        if (!cache.TryGet(id, out KVariant? variant)) return KResult.CannotFindVariant;

        string oldTitle = variant!.Title;
        KResult modelResult = variant.SetTitle(newTitle);

        if(modelResult != KResult.Completed) return modelResult; 
        
        try
        {
            await repository.UpdateAsync(variant);

            return KResult.Completed;
        }
        catch (Exception ex)
        {
            variant.SetTitle(oldTitle);
            Console.WriteLine($"{_consoleTag} Failed to update variant title for {variant}: {ex.Message}");

            return KResult.DatabaseFailure;
        }
    }

    public async Task<KResult> DeleteAsync(int id)
    {
        if (!cache.TryGet(id, out KVariant? variant)) return KResult.CannotFindVariant;

        try
        {
            await repository.DeleteAsync(id);
            cache.Remove(id);
            variant!.Character.RemoveVariant(variant);

            return KResult.Completed;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{_consoleTag} Failed to delete variant {variant}: {ex.Message}");

            return KResult.DatabaseFailure;
        }
    }
}
