using KurisuCore.Models.Bases;

namespace KurisuCore.Models.Data;

public sealed record KCardData(
    long Id,
    long? OwnerId,
    int VariantId,
    DateTime TimeObtained,
    int? BorderId,
    int? OverlayId,
    int Xp)
    : IKData<long>;
    
    