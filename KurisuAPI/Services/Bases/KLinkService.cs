using KurisuAPI.Factories;
using KurisuAPI.Repositories.Bases;
using KurisuCore.Models.Bases;
using KurisuCore.Result;

namespace KurisuAPI.Services.Bases;

public abstract class KLinkService<TFirstId, TFirst, TFirstData, TSecondId, TSecond, TSecondData>(
    KLinkRepository<TFirstId, TSecondId> repo,
    KEntityFactory factory) 
    : KService
    where TFirstId : IEquatable<TFirstId>
    where TSecondId : IEquatable<TSecondId>
    where TFirst : KEntity<TFirstId, TFirstData>, IKLinkable<TSecond>
    where TSecond : KEntity<TSecondId, TSecondData>, IKLinkable<TFirst>
    where TFirstData : IKData<TFirstId>
    where TSecondData : IKData<TSecondId>
{
    protected abstract KDetail FirstNotFoundDetail { get; }
    protected abstract KDetail SecondNotFoundDetail { get; }

    public async Task<KDetail> LinkAsync(TFirstId firstId, TSecondId secondId)
    {
        var first = await factory.LoadAsync<TFirstId, TFirst, TFirstData>(firstId);
        
        if (first is null) return FirstNotFoundDetail;
        
        var second = await factory.LoadAsync<TSecondId, TSecond, TSecondData>(secondId);

        if (second is null) return SecondNotFoundDetail;
        
        return await TryInsertAsync(
            () => repo.InsertAsync(firstId, secondId),
            () =>
            {
                first.Link(second);
                second.Link(first);
            },
            $"({first} <-> {second})");
    }


    public async Task<KDetail> UnlinkAsync(TFirstId firstId, TSecondId secondId)
    {
        var first = await factory.LoadAsync<TFirstId, TFirst, TFirstData>(firstId);
        
        if (first is null) return FirstNotFoundDetail;
        
        var second = await factory.LoadAsync<TSecondId, TSecond, TSecondData>(secondId);

        if (second is null) return SecondNotFoundDetail;
        
        return await TryDeleteAsync(
            () => repo.DeleteAsync(firstId, secondId),
            () =>
            {
                first.Unlink(second);
                second.Unlink(first);
            },
            $"({first} <-> {second})");
    } 
}

public abstract class KLinkService<TFirst, TFirstData, TSecond, TSecondData>(
    KLinkRepository repo,
    KEntityFactory factory) 
    : KLinkService<int, TFirst, TFirstData, int, TSecond, TSecondData>(repo, factory)
    where TFirst : KEntity<TFirstData>, IKLinkable<TSecond>
    where TSecond : KEntity<TSecondData>, IKLinkable<TFirst>
    where TFirstData : IKData<int>
    where TSecondData : IKData<int>;