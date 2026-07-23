using KurisuAPI.Caches;
using KurisuAPI.Factories;
using KurisuAPI.Repositories;
using KurisuAPI.Services.Bases;
using KurisuCore.Enums;
using KurisuCore.Models;
using KurisuCore.Models.Data;
using KurisuCore.Result;

namespace KurisuAPI.Services;

public sealed class KVariantService(
    KVariantRepository repo, 
    KVariantCache cache,
    KEntityFactory factory) 
    : KDataService<KVariant, KVariantData>(repo, cache, factory)
{
    protected override string ModelName => nameof(KVariant);
    protected override string ConsoleTag => "[KVariantService]";
    protected override KDetail NotFoundDetail => KDetail.VariantNotFound;

    public Task<KDetail> SetCharacterAsync(int id, KCharacter newCharacter) => SetAsync(
        id,
        p => p.Character,
        (p, v) => p.Character = v,
        newCharacter,
        "character",
        validate: (p, v) => p.Character.Equals(v) || !p.Anime.ContainsCharacter(v)
            ? KDetail.InvalidVariantCharacter
            : KDetail.Completed);

    public Task<KDetail> SetAnimeAsync(int id, KAnime newAnime) => SetAsync(
        id,
        p => p.Anime,
        (p, v) => p.Anime = v,
        newAnime,
        "anime",
        validate: (p, v) => p.Anime.Equals(v) || !p.Character.ContainsAnime(v)
            ? KDetail.InvalidVariantCharacter
            : KDetail.Completed);

    public Task<KDetail> SetRarityAsync(int id, KRarity newRarity) => SetAsync(
        id,
        p => p.Rarity,
        (p, v) => p.Rarity = v,
        newRarity,
        "rarity");

    public Task<KDetail> SetTitleAsync(int id, string newTitle) => SetAsync(
        id,
        p => p.Title,
        (p, v) => p.Title = v,
        newTitle.Trim(),
        "title",
        validate: (_, v) => string.IsNullOrWhiteSpace(v) ? KDetail.InvalidVariantTitle : KDetail.Completed);

    public Task<KDetail> SetArtistAsync(int id, string? newArtist) => SetAsync(
        id,
        p => p.Artist,
        (p, v) => p.Artist = v,
        newArtist,
        "artist");
}
