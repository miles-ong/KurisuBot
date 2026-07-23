using KurisuAPI.Caches;
using KurisuAPI.Factories;
using KurisuAPI.Repositories;
using KurisuAPI.Services.Bases;
using KurisuCore.Models;
using KurisuCore.Models.Data;
using KurisuCore.Result;

namespace KurisuAPI.Services;

public sealed class KAnimeService(
    KAnimeRepository repo,
    KAnimeCache cache,
    KEntityFactory factory) 
    : KDataSearchService<KAnime, KAnimeData>(repo, cache, factory)
{
    protected override string ModelName => nameof(KAnime);
    protected override string ConsoleTag => "[KAnimeService]";
    protected override KDetail NotFoundDetail => KDetail.AnimeNotFound;

    public Task<KDetail> SetTitleAsync(int id, string newTitle) => SetAsync(
        id,
        p => p.Title,
        (p, v) => p.Title = v,
        newTitle.Trim(),
        "title",
        validate: (_, v) => string.IsNullOrWhiteSpace(v) ? KDetail.InvalidAnimeTitle : KDetail.Completed,
        onPersisted: cache.UpdateTitle);

    public Task<KDetail> SetDescriptionAsync(int id, string newDescription) => SetAsync(
        id,
        p => p.Description,
        (p, v) => p.Description = v,
        newDescription.Trim(),
        "description",
        validate: (_, v) => string.IsNullOrWhiteSpace(v) ? KDetail.InvalidAnimeDescription : KDetail.Completed);
}