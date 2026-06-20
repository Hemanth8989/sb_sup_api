namespace StoneBridge.Application.Supplier.Warehouses.DTOs;

public sealed record WarehouseProductStockDto
{
    public Guid     Id            { get; init; }
    public Guid     WarehouseId   { get; init; }
    public Guid     VariantId     { get; init; }
    public Guid     ProductId     { get; init; }
    public string   Sku           { get; init; } = string.Empty;
    public string   VariantName   { get; init; } = string.Empty;
    public string   CategoryCode  { get; init; } = string.Empty;
    public string   CategoryLabel { get; init; } = string.Empty;
    public string   UnitOfMeasure { get; init; } = "each";
    public decimal  BasePrice     { get; init; }
    public string   Currency      { get; init; } = "USD";
    public int      QtyOnHand     { get; init; }
    public int      QtyReserved   { get; init; }
    public int      QtyAvailable  => QtyOnHand - QtyReserved;
    public string?  RackLocation  { get; init; }
    public int?     ReorderPoint  { get; init; }
    public int?     ReorderQty    { get; init; }
    public bool     IsLowStock    => ReorderPoint.HasValue && QtyOnHand <= ReorderPoint.Value;
    public string?  PrimaryPhotoUrl { get; init; }
    public DateTime UpdatedAt     { get; init; }
}

public sealed record WarehouseProductStockSummary
{
    public int      TotalSkus      { get; init; }
    public int      LowStockCount  { get; init; }
    public decimal? TotalValue     { get; init; }
}

public sealed record WarehouseProductStockFilterParams(
    string? Search       = null,
    string? CategoryCode = null,
    bool?   LowStockOnly = null,
    int     Page         = 1,
    int     PerPage      = 50
);

public sealed record ReceiveStockRequest(
    Guid    VariantId,
    int     Qty,
    string? RackLocation = null,
    string? Notes        = null
);

public sealed record TransferWarehouseStockRequest(
    Guid    VariantId,
    Guid    ToWarehouseId,
    int     Qty,
    string? ToRackLocation = null,
    string? Notes          = null
);

public sealed record AdjustWarehouseStockRequest(
    Guid    VariantId,
    int     NewQtyOnHand,
    string  Reason,
    string? Notes = null
);

public sealed record SetReorderPointRequest(
    Guid VariantId,
    int? ReorderPoint,
    int? ReorderQty
);

public sealed record StockMovementDto
{
    public Guid      Id            { get; init; }
    public Guid      VariantId     { get; init; }
    public string    VariantName   { get; init; } = string.Empty;
    public string    Sku           { get; init; } = string.Empty;
    public Guid?     FromWarehouse { get; init; }
    public string?   FromWarehouseName { get; init; }
    public Guid?     ToWarehouse   { get; init; }
    public string?   ToWarehouseName   { get; init; }
    public int       Qty           { get; init; }
    public string    MovementType  { get; init; } = string.Empty;
    public string?   Notes         { get; init; }
    public DateTime  CreatedAt     { get; init; }
}
