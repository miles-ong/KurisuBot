using KurisuCore.Enums;

namespace KurisuAPI.Records
{
    public sealed record KVariantRow(
        int Id,
        int CharacterId,
        int AnimeId,
        KRarity Rarity,
        string Title
    );
}
