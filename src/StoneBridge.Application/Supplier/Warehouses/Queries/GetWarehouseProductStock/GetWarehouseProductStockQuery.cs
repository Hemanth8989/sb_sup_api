using MediatR;
using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Application.Supplier.Warehouses.Queries.GetWarehouseProductStock;

public sealed record GetWarehouseProductStockQuery(
    Guid                             WarehouseId,
    WarehouseProductStockFilterParams Filter
) : IRequest<(IReadOnlyList<WarehouseProductStockDto> Items, int TotalCount)>;
