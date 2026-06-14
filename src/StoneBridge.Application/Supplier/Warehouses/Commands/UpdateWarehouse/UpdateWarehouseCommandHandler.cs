using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Common.Exceptions;
using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Application.Supplier.Warehouses.Commands.UpdateWarehouse;

public sealed class UpdateWarehouseCommandHandler
    : IRequestHandler<UpdateWarehouseCommand, WarehouseDto>
{
    private readonly IWarehouseRepository _repo;
    private readonly ICurrentTenant       _currentTenant;

    public UpdateWarehouseCommandHandler(IWarehouseRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public async Task<WarehouseDto> Handle(
        UpdateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var result = await _repo.UpdateAsync(
            _currentTenant.TenantId, request.WarehouseId, request.Request, cancellationToken);

        if (result is null)
        {
            throw new NotFoundException("Warehouse", request.WarehouseId);
        }

        return result;
    }
}
