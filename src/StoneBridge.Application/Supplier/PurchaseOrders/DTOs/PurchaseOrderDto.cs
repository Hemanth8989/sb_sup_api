namespace StoneBridge.Application.Supplier.PurchaseOrders.DTOs;

public sealed record PurchaseOrderDto
{
    public Guid     Id                 { get; init; }
    public string   PoNumber           { get; init; } = string.Empty;
    public Guid     FabricatorId       { get; init; }
    public string   FabricatorName     { get; init; } = string.Empty;
    public Guid     SupplierId         { get; init; }
    public Guid?    JobId              { get; init; }
    public string   Status             { get; init; } = string.Empty;
    public decimal  Subtotal           { get; init; }
    public decimal  DiscountAmount     { get; init; }
    public decimal  TaxAmount          { get; init; }
    public decimal  ShippingAmount     { get; init; }
    public decimal  TotalAmount        { get; init; }
    public string   Currency           { get; init; } = "USD";
    public DateOnly? RequestedDelivery { get; init; }
    public DateOnly? ConfirmedDelivery { get; init; }
    public string?  TrackingNumber     { get; init; }
    public string?  Carrier            { get; init; }
    public string?  FabricatorNotes    { get; init; }
    public string?  SupplierNotes      { get; init; }
    public string?  InternalRef        { get; init; }
    public DateTime? SentAt            { get; init; }
    public DateTime? AckedAt           { get; init; }
    public DateTime? ShippedAt         { get; init; }
    public DateTime? ReceivedAt        { get; init; }
    public DateTime  CreatedAt         { get; init; }
    public DateTime  UpdatedAt         { get; init; }
    public IReadOnlyList<PoLineItemDto> LineItems { get; init; } = [];
}

public sealed record PoLineItemDto
{
    public Guid     Id              { get; init; }
    public Guid     VariantId       { get; init; }
    public string   VariantName     { get; init; } = string.Empty;
    public string   Sku             { get; init; } = string.Empty;
    public Guid?    SlabId          { get; init; }
    public string?  SlabRef         { get; init; }
    public decimal  Quantity        { get; init; }
    public string   UnitOfMeasure   { get; init; } = "each";
    public decimal  UnitPrice       { get; init; }
    public decimal  LineTotal       { get; init; }
    public string   Status          { get; init; } = "pending";
    public string?  DeclineReason   { get; init; }
    public decimal? CounterPrice    { get; init; }
    public string?  CounterNote     { get; init; }
    public DateTime UpdatedAt       { get; init; }
}

public sealed record UpdatePoStatusRequest(
    string  Status,
    string? Note,
    string? SupplierNotes
);

public sealed record AcknowledgePoRequest(
    string?  SupplierNotes,
    string?  ConfirmedDelivery
);

public sealed record ShipPoRequest(
    string   TrackingNumber,
    string?  Carrier,
    string?  ConfirmedDelivery
);
