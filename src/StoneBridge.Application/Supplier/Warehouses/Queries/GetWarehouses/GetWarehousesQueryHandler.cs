using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Application.Supplier.Warehouses.Queries.GetWarehouses;

public sealed class GetWarehousesQueryHandler
    : IRequestHandler<GetWarehousesQuery, IReadOnlyList<WarehouseDto>>
{
    private readonly IWarehouseRepository _repo;
    private readonly ICurrentTenant       _currentTenant;

    public GetWarehousesQueryHandler(IWarehouseRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task<IReadOnlyList<WarehouseDto>> Handle(
        GetWarehousesQuery request, CancellationToken cancellationToken)
        => _repo.GetAllAsync(_currentTenant.TenantId, cancellationToken);
}
