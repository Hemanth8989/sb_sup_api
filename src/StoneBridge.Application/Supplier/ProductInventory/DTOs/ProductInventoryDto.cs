namespace StoneBridge.Application.Supplier.ProductInventory.DTOs;

public sealed record ProductInventoryDto
{
    public Guid     Id              { get; init; }
    public string   CategoryCode    { get; init; } = string.Empty;
    public string   CategoryLabel   { get; init; } = string.Empty;
    public string   Name            { get; init; } = string.Empty;
    public string?  Brand           { get; init; }
    public string?  Description     { get; init; }
    public bool     IsActive        { get; init; }
    public DateTime CreatedAt       { get; init; }
    public DateTime UpdatedAt       { get; init; }
    public IReadOnlyList<ProductVariantInventoryDto> Variants { get; init; } = [];
}

public sealed record ProductVariantInventoryDto
{
    public Guid     Id              { get; init; }
    public Guid     ProductId       { get; init; }
    public string   Sku             { get; init; } = string.Empty;
    public string   VariantName     { get; init; } = string.Empty;
    public string   UnitOfMeasure   { get; init; } = "each";
    public decimal  BasePrice       { get; init; }
    public string   Currency        { get; init; } = "USD";
    public int      QtyAvailable    { get; init; }
    public int      QtyReserved     { get; init; }
    public int      QtyOnHand       => QtyAvailable + QtyReserved;
    public string   Status          { get; init; } = "active";
    public int?     LeadTimeDays    { get; init; }
    public string?  PrimaryPhotoUrl { get; init; }
    public DateTime UpdatedAt       { get; init; }
}

public sealed record ProductInventoryFilterParams(
    string? CategoryCode = null,
    string? Search       = null,
    string? Status       = null,
    int     Page         = 1,
    int     PerPage      = 40
);
