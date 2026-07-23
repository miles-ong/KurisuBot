namespace KurisuCore.Models.Bases;

public interface IKEntity
{
    string DisplayName { get; }
}

public interface IKEntity<out TId> : IKEntity
{
    TId Id { get; }
}