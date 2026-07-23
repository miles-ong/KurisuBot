using KurisuCore.Models.Bases;

namespace KurisuCore.Models.Data;

public record KAnimeData(
    int Id,
    string Title,
    string? Description)
    : IKData;