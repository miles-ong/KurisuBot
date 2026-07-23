using KurisuCore.Enums;
using KurisuCore.Models.Bases;

namespace KurisuCore.Models.Data;

public sealed record KPackData(
    int Id,
    bool Enabled,
    string Name,
    KPackType PackType,
    int Price,
    int Discount,
    int FixedCardCount,
    int SrWeight,
    int SsrWeight,
    int UrWeight)
    : IKData;