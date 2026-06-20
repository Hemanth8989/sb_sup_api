using StoneBridge.Application.Common.Models;
using StoneBridge.Application.Supplier.ProductInventory.DTOs;

namespace StoneBridge.Application.Common.Interfaces;

public interface IProductInventoryRepository
{
    Task<PagedResult<ProductInventoryDto>> GetAllAsync(
        Guid tenantId, ProductInventoryFilterParams filter, CancellationToken ct = default);

    Task<ProductInventoryDto?> GetByIdAsync(Guid tenantId, Guid productId, CancellationToken ct = default);

    Task<ProductInventoryDto> UpsertAsync(Guid tenantId, UpsertProductRequest req, CancellationToken ct = default);

    Task<ProductVariantInventoryDto> AdjustStockAsync(
        Guid tenantId, Guid variantId, int delta, decimal? newPrice, CancellationToken ct = default);

    Task DeactivateAsync(Guid tenantId, Guid productId, CancellationToken ct = default);
}

public sealed record UpsertProductRequest(
    Guid?   ProductId,
    string  CategoryCode,
    string  Name,
    string? Brand,
    string? Description,
    // Variant fields
    Guid?   VariantId,
    string  Sku,
    string  VariantName,
    string  UnitOfMeasure,
    decimal BasePrice,
    int     QtyAvailable,
    int?    LeadTimeDays
);
