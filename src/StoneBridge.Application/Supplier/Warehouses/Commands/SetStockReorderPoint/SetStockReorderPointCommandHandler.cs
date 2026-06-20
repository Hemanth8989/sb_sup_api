using MediatR;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Application.Supplier.Warehouses.Commands.SetStockReorderPoint;

public sealed class SetStockReorderPointCommandHandler : IRequestHandler<SetStockReorderPointCommand>
{
    private readonly IWarehouseProductStockRepository _repo;
    private readonly ICurrentTenant _tenant;

    public SetStockReorderPointCommandHandler(
        IWarehouseProductStockRepository repo,
        ICurrentTenant tenant)
    {
        _repo   = repo;
        _tenant = tenant;
    }

    public Task Handle(SetStockReorderPointCommand request, CancellationToken cancellationToken) =>
        _repo.SetReorderPointAsync(_tenant.TenantId, request.WarehouseId, request.Request, cancellationToken);
}
