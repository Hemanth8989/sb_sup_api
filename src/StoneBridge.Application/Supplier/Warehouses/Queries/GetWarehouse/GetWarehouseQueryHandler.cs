using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Application.Supplier.Warehouses.Queries.GetWarehouse;

public sealed class GetWarehouseQueryHandler
    : IRequestHandler<GetWarehouseQuery, WarehouseDto?>
{
    private readonly IWarehouseRepository _repo;
    private readonly ICurrentTenant       _currentTenant;

    public GetWarehouseQueryHandler(IWarehouseRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task<WarehouseDto?> Handle(
        GetWarehouseQuery request, CancellationToken cancellationToken)
        => _repo.GetByIdAsync(_currentTenant.TenantId, request.WarehouseId, cancellationToken);
}
