using MediatR;

namespace StoneBridge.Application.Supplier.Warehouses.Commands.SetPrimaryWarehouse;

public sealed record SetPrimaryWarehouseCommand(Guid WarehouseId) : IRequest;
