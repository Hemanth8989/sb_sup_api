namespace StoneBridge.Application.Supplier.Warehouses.DTOs;

public sealed record WarehouseAuditEventDto
{
    public string   Id          { get; init; } = string.Empty;
    /// <summary>stock_movement | slab_transfer | slab_event</summary>
    public string   EventSource { get; init; } = string.Empty;
    public string   EventType   { get; init; } = string.Empty;
    public string   Description { get; init; } = string.Empty;
    public string?  SubjectRef  { get; init; }
    public string?  OldValue    { get; init; }
    public string?  NewValue    { get; init; }
    public string?  Notes       { get; init; }
    public decimal? Qty         { get; init; }
    public DateTime CreatedAt   { get; init; }
}
