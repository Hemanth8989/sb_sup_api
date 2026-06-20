using MediatR;
using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Application.Supplier.Warehouses.Commands.TransferWarehouseStock;

public sealed record TransferWarehouseStockCommand(Guid FromWarehouseId, TransferWarehouseStockRequest Request)
    : IRequest<(int FromQty, int ToQty)>;
