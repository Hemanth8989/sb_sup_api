using StoneBridge.Application.Common.Models;
using StoneBridge.Application.Supplier.Slabs.DTOs;

namespace StoneBridge.Application.Common.Interfaces;

public interface ISupplierSlabRepository
{
    Task<PagedResult<SupplierSlabDto>> GetInventoryAsync(
        Guid supplierId, SupplierSlabFilterParams filterParams, CancellationToken ct = default);

    Task<SupplierSlabDto?> GetByIdAsync(
        Guid supplierId, Guid slabId, CancellationToken ct = default);

    Task<SupplierSlabDto> CreateAsync(
        Guid supplierId, CreateSlabRequest request, CancellationToken ct = default);

    Task<SupplierSlabDto?> UpdateAsync(
        Guid supplierId, Guid slabId, UpdateSlabRequest request, CancellationToken ct = default);

    Task<SupplierSlabDto?> UpdateStatusAsync(
        Guid supplierId, Guid slabId, string status, CancellationToken ct = default);

    Task<SupplierSlabDto?> SetPriceOverrideAsync(
        Guid supplierId, Guid slabId, decimal? price, CancellationToken ct = default);

    Task<int> BulkUpdateStatusAsync(
        Guid supplierId, IReadOnlyList<Guid> slabIds, string status, CancellationToken ct = default);

    Task<bool> DeleteAsync(
        Guid supplierId, Guid slabId, CancellationToken ct = default);
}
