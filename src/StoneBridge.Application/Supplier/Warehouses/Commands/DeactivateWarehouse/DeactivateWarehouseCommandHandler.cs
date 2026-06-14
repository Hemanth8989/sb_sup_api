using MediatR;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Application.Supplier.Warehouses.Commands.DeactivateWarehouse;

public sealed class DeactivateWarehouseCommandHandler : IRequestHandler<DeactivateWarehouseCommand>
{
    private readonly IWarehouseRepository _repo;
    private readonly ICurrentTenant       _currentTenant;

    public DeactivateWarehouseCommandHandler(IWarehouseRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task Handle(DeactivateWarehouseCommand request, CancellationToken cancellationToken)
        => _repo.DeactivateAsync(_currentTenant.TenantId, request.WarehouseId, cancellationToken);
}
