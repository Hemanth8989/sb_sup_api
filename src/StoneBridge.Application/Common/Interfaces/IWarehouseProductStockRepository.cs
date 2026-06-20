using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Application.Common.Interfaces;

public interface IWarehouseProductStockRepository
{
    Task<(IReadOnlyList<WarehouseProductStockDto> Items, int TotalCount)> GetByWarehouseAsync(
        Guid tenantId, Guid warehouseId,
        WarehouseProductStockFilterParams filter,
        CancellationToken ct = default);

    Task<WarehouseProductStockSummary> GetSummaryAsync(
        Guid tenantId, Guid warehouseId,
        CancellationToken ct = default);

    Task<IReadOnlyList<StockMovementDto>> GetMovementsAsync(
        Guid tenantId, Guid warehouseId, int limit = 100,
        CancellationToken ct = default);

    Task<WarehouseProductStockDto> ReceiveStockAsync(
        Guid tenantId, Guid warehouseId,
        ReceiveStockRequest request,
        CancellationToken ct = default);

    Task<(int FromQty, int ToQty)> TransferStockAsync(
        Guid tenantId, Guid fromWarehouseId,
        TransferWarehouseStockRequest request,
        CancellationToken ct = default);

    Task<WarehouseProductStockDto> AdjustStockAsync(
        Guid tenantId, Guid warehouseId,
        AdjustWarehouseStockRequest request,
        CancellationToken ct = default);

    Task SetReorderPointAsync(
        Guid tenantId, Guid warehouseId,
        SetReorderPointRequest request,
        CancellationToken ct = default);

    Task<IReadOnlyList<WarehouseProductStockDto>> GetLowStockAsync(
        Guid tenantId, Guid? warehouseId = null,
        CancellationToken ct = default);
}
