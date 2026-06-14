using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Application.Supplier.Warehouses.Commands.UpdateWarehouse;

public sealed record UpdateWarehouseCommand(Guid WarehouseId, UpdateWarehouseRequest Request)
    : IRequest<WarehouseDto>;
