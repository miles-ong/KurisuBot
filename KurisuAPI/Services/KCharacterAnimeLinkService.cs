using KurisuAPI.Cache;
using KurisuAPI.Repositories;
using KurisuCore.Enums;
using KurisuCore.Models;

namespace KurisuAPI.Services;

public sealed class KCharacterAnimeLinkService
{
    private readonly KCharacterAnimeLinkRepository _repository;
    private const string _consoleTag = "[KCharacterAnimeLinkService]";

    public KCharacterAnimeLinkService(string connectionString)
    {
        _repository = new KCharacterAnimeLinkRepository(connectionString);
    }

    public async Task InitializeAsync()
    {
        var links = await _repository.GetAllAsync();
        int count = 0;

        foreach(var (cid, aid) in links)
        {
            if(!KCharacterCache.TryGet(cid, out KCharacter? character))
            {
                Console.WriteLine($"{_consoleTag} Skipping link, failed to find character (ID: {cid}) in cache.");
                continue;
            }

            if(!KAnimeCache.TryGet(aid, out KAnime? anime))
            {
                Console.WriteLine($"{_consoleTag} Skipping link, failed to find anime (ID: {aid}) in cache.");
                continue;
            }

            character!.LinkAnime(anime!);
            anime!.LinkCharacter(character);
            count++;
        }

        Console.WriteLine($"{_consoleTag} Initialized {count} links.");
    }

    public async Task<KResult> LinkAsync(int cid, int aid)
    {
        if (!KCharacterCache.TryGet(cid, out KCharacter? character)) return KResult.CannotFindCharacter;
        if (!KAnimeCache.TryGet(aid, out KAnime? anime)) return KResult.CannotFindAnime;
        if (character!.ContainsAnime(anime!)) return KResult.CharacterAnimeLinkAlreadyExists;

        try
        {
            await _repository.InsertAsync(cid, aid);
            character.LinkAnime(anime!);
            anime!.LinkCharacter(character);

            return KResult.Completed;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{_consoleTag} Failed to link character {character} to anime {anime}: {ex.Message}");

            return KResult.DatabaseFailure;
        }
    }

    public async Task<KResult> UnlinkAsync(int cid, int aid)
    {
        if (!KCharacterCache.TryGet(cid, out KCharacter? character)) return KResult.CannotFindCharacter;
        if (!KAnimeCache.TryGet(aid, out KAnime? anime)) return KResult.CannotFindAnime;
        if (!character!.ContainsAnime(anime!)) return KResult.CharacterAnimeLinkDoesNotExist;

        try
        {
            await _repository.DeleteAsync(cid, aid);
            character.UnlinkAnime(anime!);
            anime!.UnlinkCharacter(character);
            
            return KResult.Completed;
        }
        catch (KeyNotFoundException ex)
        {
            Console.WriteLine($"{_consoleTag} {ex.Message}");

            return KResult.CannotFindCharacterAnimeLink;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{_consoleTag} Failed to unlink character {character} from anime {anime}: {ex.Message}");

            return KResult.DatabaseFailure;
        }
    }
}
