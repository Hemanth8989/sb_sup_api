namespace StoneBridge.Domain.Common;

/// <summary>
/// Root base class for all domain entities.
/// ID is set on construction via gen_random_uuid() strategy —
/// the application generates the UUID, not the database.
/// This allows entities to be created in memory before being persisted.
/// </summary>
public abstract class BaseEntity
{
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
    }

    protected BaseEntity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Entity ID cannot be empty.", nameof(id));
        }

        Id = id;
    }

    public Guid Id { get; private set; }

    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(BaseEntity? a, BaseEntity? b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(BaseEntity? a, BaseEntity? b) => !(a == b);
}