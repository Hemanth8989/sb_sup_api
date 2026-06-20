namespace StoneBridge.Application.Supplier.Slabs.DTOs;

public sealed record CreateSlabRequest(
    Guid     VariantId,
    string   InternalRef,
    string   MaterialType,
    string   MaterialName,
    string   Finish,
    decimal  ThicknessCm,
    int      GrossLengthMm,
    int      GrossWidthMm,
    string   QualityGrade  = "A",
    string?  ColorFamily   = null,
    string?  Pattern       = null,
    string?  OriginCountry = null,
    string?  QuarryName    = null,
    string?  LotNumber     = null,
    string?  BlockNumber   = null,
    string?  Barcode       = null,
    decimal? WeightKg      = null,
    Guid?    BundleId      = null,
    Guid?    WarehouseId   = null,
    string?  RackLocation  = null,
    decimal? PriceOverride = null,
    bool     IsRemnant     = false,
    string?  Notes         = null
);

public sealed record UpdateSlabRequest(
    string   InternalRef,
    string   MaterialType,
    string   MaterialName,
    string   Finish,
    decimal  ThicknessCm,
    int      GrossLengthMm,
    int      GrossWidthMm,
    string   QualityGrade  = "A",
    string?  ColorFamily   = null,
    string?  Pattern       = null,
    string?  OriginCountry = null,
    string?  QuarryName    = null,
    string?  LotNumber     = null,
    string?  BlockNumber   = null,
    string?  Barcode       = null,
    decimal? WeightKg      = null,
    Guid?    BundleId      = null,
    Guid?    WarehouseId   = null,
    string?  RackLocation  = null,
    decimal? PriceOverride = null,
    bool     IsRemnant     = false,
    string?  Notes         = null
);

public sealed record BulkUpdateSlabStatusRequest(
    IReadOnlyList<Guid> SlabIds,
    string              Status
);
