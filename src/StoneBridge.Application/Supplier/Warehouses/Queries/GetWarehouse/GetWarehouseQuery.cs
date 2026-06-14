using MediatR;
using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Application.Supplier.Warehouses.Queries.GetWarehouse;

public sealed record GetWarehouseQuery(Guid WarehouseId) : IRequest<WarehouseDto?>;
