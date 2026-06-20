using MediatR;

namespace StoneBridge.Application.Supplier.Warehouses.Queries.ExportWarehouseSlabs;

public sealed record ExportWarehouseSlabsQuery(Guid WarehouseId) : IRequest<string>;
