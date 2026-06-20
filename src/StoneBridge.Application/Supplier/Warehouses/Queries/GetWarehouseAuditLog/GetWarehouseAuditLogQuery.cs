using MediatR;
using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Application.Supplier.Warehouses.Queries.GetWarehouseAuditLog;

public sealed record GetWarehouseAuditLogQuery(Guid WarehouseId, int Limit = 200)
    : IRequest<IReadOnlyList<WarehouseAuditEventDto>>;
