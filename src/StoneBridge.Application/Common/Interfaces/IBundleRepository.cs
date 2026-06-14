using StoneBridge.Application.Supplier.Bundles.DTOs;

namespace StoneBridge.Application.Common.Interfaces;

public interface IBundleRepository
{
    Task<IReadOnlyList<BundleDto>> GetAllAsync(Guid tenantId, string? search = null, CancellationToken ct = default);
    Task<BundleDetailDto?> GetByIdAsync(Guid tenantId, Guid bundleId, CancellationToken ct = default);
    Task<BundleDto> CreateAsync(Guid tenantId, CreateBundleRequest request, CancellationToken ct = default);
    Task<BundleDto> UpdateAsync(Guid tenantId, Guid bundleId, UpdateBundleRequest request, CancellationToken ct = default);
}

public sealed record CreateBundleRequest(
    string   BundleRef,
    string   MaterialName,
    string?  QuarryName,
    string?  OriginCountry,
    DateOnly? ArrivalDate,
    string?  InvoiceRef,
    string?  Notes
);

public sealed record UpdateBundleRequest(
    string   BundleRef,
    string   MaterialName,
    string?  QuarryName,
    string?  OriginCountry,
    DateOnly? ArrivalDate,
    string?  InvoiceRef,
    string?  Notes
);
