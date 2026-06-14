using MediatR;
using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Application.Supplier.Warehouses.Queries.GetWarehouses;

public sealed record GetWarehousesQuery : IRequest<IReadOnlyList<WarehouseDto>>;
