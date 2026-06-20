using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Application.Supplier.Warehouses.Queries.GetStockMovements;

public sealed class GetStockMovementsQueryHandler
    : IRequestHandler<GetStockMovementsQuery, IReadOnlyList<StockMovementDto>>
{
    private readonly IWarehouseProductStockRepository _repo;
    private readonly ICurrentTenant _tenant;

    public GetStockMovementsQueryHandler(
        IWarehouseProductStockRepository repo,
        ICurrentTenant tenant)
    {
        _repo   = repo;
        _tenant = tenant;
    }

    public Task<IReadOnlyList<StockMovementDto>> Handle(
        GetStockMovementsQuery request, CancellationToken cancellationToken) =>
        _repo.GetMovementsAsync(_tenant.TenantId, request.WarehouseId, request.Limit, cancellationToken);
}
