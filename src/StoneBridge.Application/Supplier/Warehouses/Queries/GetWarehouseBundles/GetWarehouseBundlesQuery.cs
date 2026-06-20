using MediatR;
using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Application.Supplier.Warehouses.Queries.GetWarehouseBundles;

public sealed record GetWarehouseBundlesQuery(Guid WarehouseId)
    : IRequest<IReadOnlyList<WarehouseBundleDto>>;
