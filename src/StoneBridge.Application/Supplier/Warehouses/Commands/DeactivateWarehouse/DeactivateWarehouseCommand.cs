using MediatR;

namespace StoneBridge.Application.Supplier.Warehouses.Commands.DeactivateWarehouse;

public sealed record DeactivateWarehouseCommand(Guid WarehouseId) : IRequest;
