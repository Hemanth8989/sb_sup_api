using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Common.Exceptions;
using StoneBridge.Application.Supplier.PurchaseOrders.DTOs;

namespace StoneBridge.Application.Supplier.PurchaseOrders.Commands.ShipPo;

public sealed class ShipPoCommandHandler : IRequestHandler<ShipPoCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _repo;
    private readonly ICurrentTenant           _currentTenant;

    public ShipPoCommandHandler(IPurchaseOrderRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public async Task<PurchaseOrderDto> Handle(ShipPoCommand request, CancellationToken cancellationToken)
    {
        var result = await _repo.ShipAsync(
            _currentTenant.TenantId, request.PoId, request.Request, cancellationToken);

        if (result is null)
        {
            throw new NotFoundException("PurchaseOrder", request.PoId);
        }

        return result;
    }
}
