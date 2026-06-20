using MediatR;
using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Application.Supplier.Warehouses.Commands.SetStockReorderPoint;

public sealed record SetStockReorderPointCommand(Guid WarehouseId, SetReorderPointRequest Request)
    : IRequest;
