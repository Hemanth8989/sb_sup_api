using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Application.Supplier.Warehouses.Queries.GetWarehouseBundles;

internal sealed class GetWarehouseBundlesQueryHandler
    : IRequestHandler<GetWarehouseBundlesQuery, IReadOnlyList<WarehouseBundleDto>>
{
    private readonly ICurrentTenant       _tenant;
    private readonly IWarehouseRepository _repo;

    public GetWarehouseBundlesQueryHandler(ICurrentTenant tenant, IWarehouseRepository repo)
    {
        _tenant = tenant;
        _repo   = repo;
    }

    public Task<IReadOnlyList<WarehouseBundleDto>> Handle(
        GetWarehouseBundlesQuery request, CancellationToken cancellationToken)
        => _repo.GetBundlesAsync(_tenant.TenantId, request.WarehouseId, cancellationToken);
}
