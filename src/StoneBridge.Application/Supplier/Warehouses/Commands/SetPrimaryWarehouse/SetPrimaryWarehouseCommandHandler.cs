using MediatR;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Application.Supplier.Warehouses.Commands.SetPrimaryWarehouse;

public sealed class SetPrimaryWarehouseCommandHandler : IRequestHandler<SetPrimaryWarehouseCommand>
{
    private readonly IWarehouseRepository _repo;
    private readonly ICurrentTenant       _currentTenant;

    public SetPrimaryWarehouseCommandHandler(IWarehouseRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task Handle(SetPrimaryWarehouseCommand request, CancellationToken cancellationToken)
        => _repo.SetPrimaryAsync(_currentTenant.TenantId, request.WarehouseId, cancellationToken);
}
