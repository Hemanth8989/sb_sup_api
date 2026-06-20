namespace StoneBridge.Application.Supplier.Warehouses.DTOs;

public sealed record WarehouseBundleDto
{
    public string?  BundleId        { get; init; }
    /// <summary>bundle_ref from slab_bundles, lot_number on slab, or null for ungrouped.</summary>
    public string?  GroupRef        { get; init; }
    /// <summary>bundle | lot | ungrouped</summary>
    public string   GroupType       { get; init; } = string.Empty;
    public string   MaterialName    { get; init; } = string.Empty;
    public string?  OriginCountry   { get; init; }
    public string?  QuarryName      { get; init; }
    public string?  ArrivalDate     { get; init; }
    public string?  InvoiceRef      { get; init; }
    public int      SlabCount       { get; init; }
    public int      AvailableCount  { get; init; }
    public int      ReservedCount   { get; init; }
    public int      OnHoldCount     { get; init; }
    public decimal? TotalSqft       { get; init; }
    public decimal? EstimatedValue  { get; init; }
    public DateTime FirstReceivedAt { get; init; }
}
