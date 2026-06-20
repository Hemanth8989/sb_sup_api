using MediatR;
using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Application.Supplier.Warehouses.Commands.AdjustWarehouseStock;

public sealed record AdjustWarehouseStockCommand(Guid WarehouseId, AdjustWarehouseStockRequest Request)
    : IRequest<WarehouseProductStockDto>;
