namespace StoneBridge.Application.Supplier.Bundles.DTOs;

public sealed record BundleDto
{
    public Guid      Id             { get; init; }
    public string    BundleRef      { get; init; } = string.Empty;
    public string    MaterialName   { get; init; } = string.Empty;
    public string?   QuarryName     { get; init; }
    public string?   OriginCountry  { get; init; }
    public DateOnly? ArrivalDate    { get; init; }
    public string?   InvoiceRef     { get; init; }
    public string?   Notes          { get; init; }
    public int       SlabCount      { get; init; }
    public int       ActiveCount    { get; init; }
    public int       AvailableCount { get; init; }
    public decimal?  TotalSqftAvailable { get; init; }
    public DateTime  CreatedAt      { get; init; }
    public DateTime  UpdatedAt      { get; init; }
}

public sealed record BundleSlabDto
{
    public Guid     Id              { get; init; }
    public string   InternalRef     { get; init; } = string.Empty;
    public string?  BlockNumber     { get; init; }
    public decimal  ThicknessCm     { get; init; }
    public string   Finish          { get; init; } = string.Empty;
    public int      GrossLengthMm   { get; init; }
    public int      GrossWidthMm    { get; init; }
    public decimal  NetSqft         { get; init; }
    public string   QualityGrade    { get; init; } = "A";
    public string   Status          { get; init; } = string.Empty;
    public string?  RackLocation    { get; init; }
    public string?  WarehouseName   { get; init; }
    public decimal? PriceOverride   { get; init; }
    public string?  PrimaryPhotoUrl { get; init; }
    public DateTime UpdatedAt       { get; init; }
}

public sealed record BundleDetailDto
{
    public Guid      Id                  { get; init; }
    public string    BundleRef           { get; init; } = string.Empty;
    public string    MaterialName        { get; init; } = string.Empty;
    public string?   QuarryName          { get; init; }
    public string?   OriginCountry       { get; init; }
    public DateOnly? ArrivalDate         { get; init; }
    public string?   InvoiceRef          { get; init; }
    public string?   Notes               { get; init; }
    public int       SlabCount           { get; init; }
    public int       ActiveCount         { get; init; }
    public int       AvailableCount      { get; init; }
    public decimal?  TotalSqftAvailable  { get; init; }
    public DateTime  CreatedAt           { get; init; }
    public DateTime  UpdatedAt           { get; init; }
    public IReadOnlyList<BundleSlabDto> Slabs { get; init; } = [];
}
