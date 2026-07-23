using KurisuAPI.Caches;
using KurisuAPI.Caches.Bases;
using KurisuAPI.Repositories;
using KurisuAPI.Repositories.Bases;
using KurisuCore.Models;
using KurisuCore.Models.Bases;
using KurisuCore.Models.Data;

namespace KurisuAPI.Factories;

public sealed class KEntityFactory
{
    private readonly KAnimeRepository _animeRepo;
    private readonly KCharacterRepository _characterRepo;
    private readonly KVariantRepository _variantRepo;
    private readonly KPackRepository _packRepo;
    private readonly KCharacterAnimeLinkRepository _caLinkRepo;
    private readonly KVariantPackLinkRepository _vpLinkRepo;

    private readonly KAnimeCache _animeCache;
    private readonly KCharacterCache _characterCache;
    private readonly KVariantCache _variantCache;
    private readonly KPackCache _packCache;
    
    private readonly Dictionary<Type, object> _repos;
    private readonly Dictionary<Type, object> _caches;
    private readonly Dictionary<Type, Delegate> _builders;
    private readonly Dictionary<Type, Delegate> _destroyers;

    public KEntityFactory(
        KAnimeRepository animeRepo,
        KCharacterRepository characterRepo,
        KVariantRepository variantRepo,
        KPackRepository packRepo,
        KCharacterAnimeLinkRepository caLinkRepo,
        KVariantPackLinkRepository vpLinkRepo,
        KAnimeCache animeCache,
        KCharacterCache characterCache,
        KVariantCache variantCache,
        KPackCache packCache)
    {
        _animeRepo = animeRepo;
        _characterRepo = characterRepo;
        _variantRepo = variantRepo;
        _packRepo = packRepo;
        _caLinkRepo = caLinkRepo;
        _vpLinkRepo = vpLinkRepo;
        _animeCache = animeCache;
        _characterCache = characterCache;
        _variantCache = variantCache;
        _packCache = packCache;

        _repos = new Dictionary<Type, object>()
        {
            [typeof(KAnimeData)] = animeRepo,
            [typeof(KCharacterData)] = characterRepo,
            [typeof(KVariantData)] = variantRepo,
            [typeof(KPackData)] = packRepo
        };

        _caches = new Dictionary<Type, object>()
        {
            [typeof(KAnimeData)] = animeCache,
            [typeof(KCharacterData)] = characterCache,
            [typeof(KVariantData)] = variantCache,
            [typeof(KPackData)] = packCache
        };

        _builders = new Dictionary<Type, Delegate>()
        {
            [typeof(KAnimeData)] = BuildAnimeAsync,
            [typeof(KCharacterData)] = BuildCharacterAsync,
            [typeof(KVariantData)] = BuildVariantAsync,
            [typeof(KPackData)] = BuildPackAsync
        };

        _destroyers = new Dictionary<Type, Delegate>()
        {
            [typeof(KAnime)] = DestroyAnime,
            [typeof(KCharacter)] = DestroyCharacter,
            [typeof(KVariant)] = DestroyVariant,
            [typeof(KPack)] = DestroyPack
        };
    }
    
    public async Task<TEntity?> LoadAsync<TId, TEntity, TData>(TId id)
        where TId : IEquatable<TId>
        where TEntity : class, IKEntity<TId>
        where TData : IKData<TId>
    {
        var repo = GetRepository<TId, TData>();
        var cache = GetCache<TId, TEntity>();
        
        if (cache.TryGet(id, out var cached)) return cached!;

        var data = await repo.GetByIdAsync(id);

        if (data is null) return null; // potential fix here remove null possibility add result maybe?

        return await BuildAsync<TId, TEntity, TData>(id, data);
    }

    public Task<TEntity> BuildAsync<TId, TEntity, TData>(TId id, TData data)
        where TId : IEquatable<TId>
        where TEntity : class, IKEntity<TId>
        where TData : IKData<TId>
    {
        if (!_builders.TryGetValue(typeof(TData), out var builder))
            throw new InvalidOperationException($"No builder registered for {typeof(TData).Name}.");

        if (builder is Func<TId, TData, Task<TEntity>> typed) return typed(id, data);

        throw new InvalidOperationException($"Builder mismatch for {typeof(TData).Name}.");
    }

    public void Destroy<TEntity>(TEntity entity) where TEntity : IKEntity
    {
        if (!_destroyers.TryGetValue(typeof(TEntity), out var destroyer))
            throw new InvalidOperationException($"No destroyer registered for {typeof(TEntity).Name}.");

        if (destroyer is not Action<TEntity> typed) throw new InvalidOperationException($"Destroyer mismatch for {typeof(TEntity).Name}.");

        typed(entity);
    }
    
    private KDataRepository<TId, TData> GetRepository<TId, TData>()
        where TId : IEquatable<TId>
        where TData : IKData<TId>
    {
        if (_repos.TryGetValue(typeof(TData), out var repo)) 
            return (KDataRepository<TId, TData>) repo;
        
        throw new InvalidOperationException($"No repository registered for {typeof(TData).Name}.");
    }

    private IKCache<TId, TEntity> GetCache<TId, TEntity>()
        where TId : IEquatable<TId>
        where TEntity : class, IKEntity<TId>
    {
        if (_caches.TryGetValue(typeof(TEntity), out var cache))
            return (IKCache<TId, TEntity>) cache;
        
        throw new InvalidOperationException($"No cache registered for {typeof(TEntity).Name}.");
    }

    #region Loaders
    private Task<KAnime?> LoadAnimeAsync(int id) => LoadAsync<int, KAnime, KAnimeData>(id);
    private Task<KCharacter?> LoadCharacterAsync(int id) => LoadAsync<int, KCharacter, KCharacterData>(id);
    private Task<KVariant?> LoadVariantAsync(int id) => LoadAsync<int, KVariant, KVariantData>(id);
    private Task<KPack?> LoadPackAsync(int id) => LoadAsync<int, KPack, KPackData>(id);
    #endregion
    
    #region Builders
    private async Task<KAnime> BuildAnimeAsync(int id, KAnimeData data)
    {
        if (_animeCache.TryGet(id, out var cached)) return cached!;

        var anime = new KAnime(data.Title, data.Description);
        anime.SetId(id);
        _animeCache.Add(anime);

        foreach (var cid in await _caLinkRepo.GetByAnimeIdAsync(id))
        {
            var character = await LoadCharacterAsync(cid);

            if (character is null) continue;

            ((IKLinkable<KCharacter>)anime).Link(character);
            ((IKLinkable<KAnime>)character).Link(anime);
        }

        return anime;
    }
    
    private async Task<KCharacter> BuildCharacterAsync(int id, KCharacterData data)
    {
        if (_characterCache.TryGet(id, out var cached)) return cached!;

        var character = new KCharacter(data.Name);
        character.SetId(id);
        _characterCache.Add(character);

        foreach (var aid in await _caLinkRepo.GetByCharacterIdAsync(id))
        {
            var anime = await LoadAnimeAsync(aid);

            if (anime is null) continue;

            ((IKLinkable<KAnime>)character).Link(anime);
            ((IKLinkable<KCharacter>)anime).Link(character);
        }

        foreach (var variantData in await _variantRepo.GetByCharacterIdAsync(id))
        {
            await LoadVariantAsync(variantData.Id);
        }

        return character;
    }
    
    private async Task<KVariant> BuildVariantAsync(int id, KVariantData data)
    {
        if (_variantCache.TryGet(id, out var cached)) return cached!;

        var character = await LoadCharacterAsync(data.CharacterId);
        var anime = await LoadAnimeAsync(data.AnimeId);

        if (character is null || anime is null) return null!;

        var variant = new KVariant(
            character,
            anime,
            data.Rarity,
            data.Title,
            data.Artist);

        variant.SetId(id);
        _variantCache.Add(variant);
        character.AddVariant(variant);

        foreach (var pid in await _vpLinkRepo.GetByVariantIdAsync(id))
        {
            var pack = await LoadPackAsync(pid);

            if (pack is null) continue;

            ((IKLinkable<KPack>)variant).Link(pack);
            ((IKLinkable<KVariant>)pack).Link(variant);
        }

        return variant;
    }
    
    private async Task<KPack> BuildPackAsync(int id, KPackData data)
    {
        if (_packCache.TryGet(id, out var cached)) return cached!;

        var pack = new KPack(
            data.Enabled,
            data.Name,
            data.PackType,
            data.Price,
            data.Discount,
            data.FixedCardCount,
            data.SrWeight,
            data.SsrWeight,
            data.UrWeight);

        pack.SetId(id);
        _packCache.Add(pack);

        foreach (var vid in await _vpLinkRepo.GetByPackIdAsync(id))
        {
            var variant = await LoadVariantAsync(vid);

            if (variant is null) continue;

            ((IKLinkable<KVariant>)pack).Link(variant);
            ((IKLinkable<KPack>)variant).Link(pack);
        }

        return pack;
    }
    #endregion
    
    #region Destroyers
    private void DestroyAnime(KAnime anime)
    {
        foreach (var c in anime.Characters)
        {
            ((IKLinkable<KAnime>)c).Unlink(anime);
        }

        _animeCache.Remove(anime.Id);
    }

    private void DestroyCharacter(KCharacter character)
    {
        foreach (var a in character.Anime)
        {
            ((IKLinkable<KCharacter>)a).Unlink(character);
        }

        foreach (var v in character.Variants)
        {
            DestroyVariant(v);
        }
        
        _characterCache.Remove(character.Id);
    }

    private void DestroyVariant(KVariant variant)
    {
        foreach (var p in variant.Packs)
        {
            ((IKLinkable<KVariant>)p).Unlink(variant);
        }
        
        variant.Character.RemoveVariant(variant);
        _variantCache.Remove(variant.Id);
    }

    private void DestroyPack(KPack pack)
    {
        foreach (var v in pack.Variants)
        {
            ((IKLinkable<KPack>)v).Unlink(pack);
        }

        _packCache.Remove(pack.Id);
    }
    #endregion
}