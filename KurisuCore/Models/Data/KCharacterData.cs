using KurisuCore.Models.Bases;

namespace KurisuCore.Models.Data;

public record KCharacterData(
    int Id,
    string Name)
    : IKData;