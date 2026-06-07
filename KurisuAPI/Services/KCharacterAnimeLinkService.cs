using KurisuAPI.Repositories;
using KurisuCore.Enums;
using KurisuCore.Models;

namespace KurisuAPI.Services;

public sealed class KCharacterAnimeLinkService(
    KCharacterAnimeLinkRepository repository, 
    KAnimeService animeService, 
    KCharacterService characterService
    )
{
    private const string _consoleTag = "[KCharacterAnimeLinkService]";

    public async Task LoadLinksAsync(KAnime anime)
    {
        List<int> cids = await repository.GetByAnimeIdAsync(anime.Id);

        foreach (int cid in cids)
        {
            KCharacter? character = await characterService.GetByIdAsync(cid);

            if (character is null) continue;

            anime.LinkCharacter(character);
            character.LinkAnime(anime);
        }
    }

    public async Task LoadLinksAsync(KCharacter character)
    {
        List<int> aids = await repository.GetByCharacterIdAsync(character.Id);

        foreach (int aid in aids)
        {
            KAnime? anime = await animeService.GetByIdAsync(aid);

            if (anime is null) continue;

            anime.LinkCharacter(character);
            character.LinkAnime(anime);
        }
    }

    public async Task<KResult> LinkAsync(int cid, int aid)
    {
        KCharacter? character = await characterService.GetByIdAsync(cid);
        KAnime? anime = await animeService.GetByIdAsync(aid);

        if (character is null) return KResult.CannotFindCharacter;
        if (anime is null) return KResult.CannotFindAnime;
        if (character.ContainsAnime(anime)) return KResult.CharacterAnimeLinkAlreadyExists;

        try
        {
            await repository.InsertAsync(cid, aid);
            character.LinkAnime(anime);
            anime.LinkCharacter(character);

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
        KCharacter? character = await characterService.GetByIdAsync(cid);
        KAnime? anime = await animeService.GetByIdAsync(aid);

        if (character is null) return KResult.CannotFindCharacter;
        if (anime is null) return KResult.CannotFindAnime;
        if (!character.ContainsAnime(anime)) return KResult.CharacterAnimeLinkDoesNotExist;

        try
        {
            await repository.DeleteAsync(cid, aid);
            character.UnlinkAnime(anime);
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
