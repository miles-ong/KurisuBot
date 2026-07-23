using KurisuAPI.Caches;
using KurisuAPI.Factories;
using KurisuAPI.Repositories;
using KurisuAPI.Services.Bases;
using KurisuCore.Models;
using KurisuCore.Models.Data;
using KurisuCore.Result;

namespace KurisuAPI.Services;

public sealed class KCardService(
    KCardRepository repo,
    KCardCache cache,
    KEntityFactory factory)
    : KDataSaveService<KCard, KCardData>(repo, cache, factory)
{
    protected override string ModelName => nameof(KCard);
    protected override string ConsoleTag => "[KCardService]";
    protected override KDetail NotFoundDetail => KDetail.CardNotFound;
    protected override int SaveLoopIntervalMinutes => 1;
}
