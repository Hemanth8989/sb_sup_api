using MediatR;
using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Application.Supplier.Warehouses.Commands.ReceiveWarehouseStock;

public sealed record ReceiveWarehouseStockCommand(Guid WarehouseId, ReceiveStockRequest Request)
    : IRequest<WarehouseProductStockDto>;
