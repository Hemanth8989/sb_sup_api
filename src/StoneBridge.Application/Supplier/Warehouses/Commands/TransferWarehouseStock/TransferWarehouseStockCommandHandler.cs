using MediatR;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Application.Supplier.Warehouses.Commands.TransferWarehouseStock;

public sealed class TransferWarehouseStockCommandHandler
    : IRequestHandler<TransferWarehouseStockCommand, (int FromQty, int ToQty)>
{
    private readonly IWarehouseProductStockRepository _repo;
    private readonly ICurrentTenant _tenant;

    public TransferWarehouseStockCommandHandler(
        IWarehouseProductStockRepository repo,
        ICurrentTenant tenant)
    {
        _repo   = repo;
        _tenant = tenant;
    }

    public Task<(int FromQty, int ToQty)> Handle(
        TransferWarehouseStockCommand request, CancellationToken cancellationToken) =>
        _repo.TransferStockAsync(_tenant.TenantId, request.FromWarehouseId, request.Request, cancellationToken);
}
