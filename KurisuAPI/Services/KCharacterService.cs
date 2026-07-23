using KurisuAPI.Caches;
using KurisuAPI.Factories;
using KurisuAPI.Repositories;
using KurisuAPI.Services.Bases;
using KurisuCore.Models;
using KurisuCore.Models.Data;
using KurisuCore.Result;

namespace KurisuAPI.Services;

public sealed class KCharacterService(
    KCharacterRepository repo, 
    KCharacterCache cache,
    KEntityFactory factory)
    : KDataSearchService<KCharacter, KCharacterData>(repo, cache, factory)
{
    protected override string ModelName => nameof(KCharacter);
    protected override string ConsoleTag => "[KCharacterService]";
    protected override KDetail NotFoundDetail => KDetail.CharacterNotFound;

    public Task<KDetail> SetNameAsync(int id, string newName) => SetAsync(
        id,
        p => p.Name,
        (p, v) => p.Name = v,
        newName,
        "name",
        validate: (_, v) => string.IsNullOrWhiteSpace(v.Trim()) ? KDetail.InvalidAnimeTitle : KDetail.Completed,
        onPersisted: cache.UpdateName);
}
