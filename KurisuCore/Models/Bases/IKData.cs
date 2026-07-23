namespace KurisuCore.Models.Bases;

public interface IKData<out TId>
{
    TId Id { get; }
};

public interface IKData : IKData<int>;