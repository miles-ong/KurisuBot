using KurisuAPI.Factories;
using KurisuAPI.Repositories;
using KurisuAPI.Services.Bases;
using KurisuCore.Models;
using KurisuCore.Models.Data;
using KurisuCore.Result;

namespace KurisuAPI.Services;

public sealed class KCharacterAnimeLinkService(
    KCharacterAnimeLinkRepository repo,
    KEntityFactory factory) 
    : KLinkService<KCharacter, KCharacterData, KAnime, KAnimeData>(repo, factory)
{
    protected override string ModelName => "CharacterAnimeLink";
    protected override string ConsoleTag => "[KCharacterAnimeLinkService]";
    protected override KDetail NotFoundDetail => KDetail.CharacterAnimeLinkNotFound;
    protected override KDetail FirstNotFoundDetail => KDetail.CharacterNotFound;
    protected override KDetail SecondNotFoundDetail => KDetail.AnimeNotFound;
}
