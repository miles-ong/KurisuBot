using KurisuCore.Enums;
using KurisuCore.Models.Bases;

namespace KurisuCore.Models.Data;

public sealed record KVariantData(
    int Id,
    int CharacterId,
    int AnimeId,
    KRarity Rarity,
    string Title,
    string? Artist)
    : IKData;