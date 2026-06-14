using MediatR;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Application.Supplier.Warehouses.Commands.TransferSlabs;

public sealed class TransferSlabsCommandHandler : IRequestHandler<TransferSlabsCommand, int>
{
    private readonly IWarehouseRepository _repo;
    private readonly ICurrentTenant       _currentTenant;

    public TransferSlabsCommandHandler(IWarehouseRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task<int> Handle(TransferSlabsCommand request, CancellationToken cancellationToken)
        => _repo.TransferSlabsAsync(
            _currentTenant.TenantId,
            request.Request.SlabIds,
            request.Request.TargetWarehouseId,
            request.Request.RackLocation,
            cancellationToken);
}
