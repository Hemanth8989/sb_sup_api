using StoneBridge.Application.Common.Models;
using StoneBridge.Application.Supplier.Slabs.DTOs;

namespace StoneBridge.Application.Common.Interfaces;

/// <summary>
/// Data access contract for the supplier's inventory management.
/// Implementations must only return slabs owned by the requesting supplier (tenant_id = supplierId).
/// PostgreSQL RLS provides enforcement via app.tenant_id session context.
/// </summary>
public interface ISupplierSlabRepository
{
    /// <summary>
    /// Returns a paginated, filtered list of the supplier's own slabs.
    /// Unlike the catalog, this includes ALL statuses (available, reserved, allocated, etc.)
    /// so suppliers can see their full inventory state.
    /// </summary>
    Task<PagedResult<SupplierSlabDto>> GetInventoryAsync(
        Guid                    supplierId,
        SupplierSlabFilterParams filterParams,
        CancellationToken        ct = default);
}
