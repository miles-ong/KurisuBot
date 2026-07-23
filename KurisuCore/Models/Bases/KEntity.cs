namespace KurisuCore.Models.Bases;

public abstract class KEntity<TId, TData> : IKEntity<TId>
    where TId : IEquatable<TId>
    where TData : IKData<TId>
{
    public TId Id { get; private set; } = default!;
    public abstract string DisplayName { get; }
    
    internal void SetId(TId id) =>
        Id = Id.Equals(default) ? id : throw new InvalidOperationException("Id has already been assigned.");

    internal abstract TData ToData(); 
    public override bool Equals(object? obj) => obj is KEntity<TId, TData> other && Id.Equals(other.Id);
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}

public abstract class KEntity<TData> : KEntity<int, TData> where TData : IKData<int>;