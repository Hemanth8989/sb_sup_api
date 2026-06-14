namespace StoneBridge.Application.Supplier.Warehouses.DTOs;

public sealed record WarehouseDto
{
    public Guid     Id             { get; init; }
    public string   Name           { get; init; } = string.Empty;
    public string?  AddressLine1   { get; init; }
    public string?  City           { get; init; }
    public string?  StateProvince  { get; init; }
    public string?  PostalCode     { get; init; }
    public string   Country        { get; init; } = "US";
    public string?  Phone          { get; init; }
    public bool     IsPrimary      { get; init; }
    public bool     IsActive       { get; init; }
    public int      SlabCount      { get; init; }
    public int      AvailableCount { get; init; }
    public int      ReservedCount  { get; init; }
    public int      OnHoldCount    { get; init; }
    public decimal? EstimatedValue { get; init; }
    public DateTime CreatedAt      { get; init; }
    public DateTime UpdatedAt      { get; init; }
}
