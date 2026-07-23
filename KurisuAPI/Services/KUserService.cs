using KurisuAPI.Caches;
using KurisuAPI.Factories;
using KurisuAPI.Repositories;
using KurisuAPI.Services.Bases;
using KurisuCore.Models;
using KurisuCore.Models.Data;
using KurisuCore.Result;

namespace KurisuAPI.Services;

public sealed class KUserService(
    KUserRepository repo, 
    KUserCache cache,
    KEntityFactory factory)
    : KDataSaveService<KUser, KUserData>(repo, cache, factory)
{
    protected override string ModelName => nameof(KUser);
    protected override string ConsoleTag => "[KUserService]";
    protected override KDetail NotFoundDetail => KDetail.UserNotFound;
    protected override int SaveLoopIntervalMinutes => 1;
}
