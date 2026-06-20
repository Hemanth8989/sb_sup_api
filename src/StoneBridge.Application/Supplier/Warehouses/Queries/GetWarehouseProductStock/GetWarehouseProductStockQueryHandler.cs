using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Application.Supplier.Warehouses.Queries.GetWarehouseProductStock;

public sealed class GetWarehouseProductStockQueryHandler
    : IRequestHandler<GetWarehouseProductStockQuery, (IReadOnlyList<WarehouseProductStockDto> Items, int TotalCount)>
{
    private readonly IWarehouseProductStockRepository _repo;
    private readonly ICurrentTenant _tenant;

    public GetWarehouseProductStockQueryHandler(
        IWarehouseProductStockRepository repo,
        ICurrentTenant tenant)
    {
        _repo   = repo;
        _tenant = tenant;
    }

    public Task<(IReadOnlyList<WarehouseProductStockDto> Items, int TotalCount)> Handle(
        GetWarehouseProductStockQuery request,
        CancellationToken cancellationToken) =>
        _repo.GetByWarehouseAsync(_tenant.TenantId, request.WarehouseId, request.Filter, cancellationToken);
}
