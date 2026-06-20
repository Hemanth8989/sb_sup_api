using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Application.Supplier.Warehouses.Queries.GetWarehouseAuditLog;

internal sealed class GetWarehouseAuditLogQueryHandler
    : IRequestHandler<GetWarehouseAuditLogQuery, IReadOnlyList<WarehouseAuditEventDto>>
{
    private readonly ICurrentTenant      _tenant;
    private readonly IWarehouseRepository _repo;

    public GetWarehouseAuditLogQueryHandler(ICurrentTenant tenant, IWarehouseRepository repo)
    {
        _tenant = tenant;
        _repo   = repo;
    }

    public Task<IReadOnlyList<WarehouseAuditEventDto>> Handle(
        GetWarehouseAuditLogQuery request, CancellationToken cancellationToken)
        => _repo.GetAuditLogAsync(_tenant.TenantId, request.WarehouseId, request.Limit, cancellationToken);
}
