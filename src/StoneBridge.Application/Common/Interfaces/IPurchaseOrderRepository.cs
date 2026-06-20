using StoneBridge.Application.Supplier.PurchaseOrders.DTOs;
using StoneBridge.Application.Common.Models;

namespace StoneBridge.Application.Common.Interfaces;

public interface IPurchaseOrderRepository
{
    Task<PagedResult<PurchaseOrderDto>> GetAllAsync(Guid supplierId, PoFilterParams filter, CancellationToken ct = default);
    Task<PurchaseOrderDto?> GetByIdAsync(Guid supplierId, Guid poId, CancellationToken ct = default);
    Task<PurchaseOrderDto> AcknowledgeAsync(Guid supplierId, Guid poId, AcknowledgePoRequest req, CancellationToken ct = default);
    Task<PurchaseOrderDto> ShipAsync(Guid supplierId, Guid poId, ShipPoRequest req, CancellationToken ct = default);
    Task<PurchaseOrderDto> UpdateStatusAsync(Guid supplierId, Guid poId, UpdatePoStatusRequest req, CancellationToken ct = default);
}

public sealed record PoFilterParams(
    string?  Status    = null,
    int      Page      = 1,
    int      PerPage   = 25
);
