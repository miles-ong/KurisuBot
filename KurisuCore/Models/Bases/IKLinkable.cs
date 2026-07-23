namespace KurisuCore.Models.Bases;

public interface IKLinkable<in TEntity>
{
    internal void Link(TEntity entity);
    internal void Unlink(TEntity entity);
} 