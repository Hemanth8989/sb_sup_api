using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Application.Supplier.Warehouses.Commands.ReceiveWarehouseStock;

public sealed class ReceiveWarehouseStockCommandHandler
    : IRequestHandler<ReceiveWarehouseStockCommand, WarehouseProductStockDto>
{
    private readonly IWarehouseProductStockRepository _repo;
    private readonly ICurrentTenant _tenant;

    public ReceiveWarehouseStockCommandHandler(
        IWarehouseProductStockRepository repo,
        ICurrentTenant tenant)
    {
        _repo   = repo;
        _tenant = tenant;
    }

    public Task<WarehouseProductStockDto> Handle(
        ReceiveWarehouseStockCommand request, CancellationToken cancellationToken) =>
        _repo.ReceiveStockAsync(_tenant.TenantId, request.WarehouseId, request.Request, cancellationToken);
}
