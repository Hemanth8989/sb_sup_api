using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Application.Supplier.Warehouses.Commands.CreateWarehouse;

public sealed class CreateWarehouseCommandHandler
    : IRequestHandler<CreateWarehouseCommand, WarehouseDto>
{
    private readonly IWarehouseRepository _repo;
    private readonly ICurrentTenant       _currentTenant;

    public CreateWarehouseCommandHandler(IWarehouseRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task<WarehouseDto> Handle(
        CreateWarehouseCommand request, CancellationToken cancellationToken)
        => _repo.CreateAsync(_currentTenant.TenantId, request.Request, cancellationToken);
}
