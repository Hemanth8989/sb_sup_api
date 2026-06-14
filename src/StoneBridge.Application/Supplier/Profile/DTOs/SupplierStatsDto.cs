namespace StoneBridge.Application.Supplier.Profile.DTOs;

public sealed record SupplierStatsDto
{
    public decimal? AvgLeadDays      { get; init; }
    public decimal? FulfillmentRate  { get; init; }
    public decimal? AvgResponseHrs   { get; init; }
    public int      TotalSlabsSold   { get; init; }
    public int      WarehouseCount   { get; init; }
    public DateTime? UpdatedAt       { get; init; }
}
