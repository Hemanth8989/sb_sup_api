namespace StoneBridge.Domain.Common;

/// <summary>
/// Extends BaseEntity with created_at / updated_at audit fields.
/// updated_at is maintained by a PostgreSQL trigger (trg_set_updated_at)
/// so the application sets it on creation only.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    protected AuditableEntity() : base() { }
    protected AuditableEntity(Guid id) : base(id) { }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
}