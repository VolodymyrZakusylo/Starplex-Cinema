namespace StarPlex.Domain.Common;

public abstract class BaseEntity
{
    public virtual Guid Id { get; set; } = Guid.NewGuid();
}