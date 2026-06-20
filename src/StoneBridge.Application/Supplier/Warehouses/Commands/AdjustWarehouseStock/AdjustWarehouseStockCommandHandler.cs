using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Application.Supplier.Warehouses.Commands.AdjustWarehouseStock;

public sealed class AdjustWarehouseStockCommandHandler
    : IRequestHandler<AdjustWarehouseStockCommand, WarehouseProductStockDto>
{
    private readonly IWarehouseProductStockRepository _repo;
    private readonly ICurrentTenant _tenant;

    public AdjustWarehouseStockCommandHandler(
        IWarehouseProductStockRepository repo,
        ICurrentTenant tenant)
    {
        _repo   = repo;
        _tenant = tenant;
    }

    public Task<WarehouseProductStockDto> Handle(
        AdjustWarehouseStockCommand request, CancellationToken cancellationToken) =>
        _repo.AdjustStockAsync(_tenant.TenantId, request.WarehouseId, request.Request, cancellationToken);
}
