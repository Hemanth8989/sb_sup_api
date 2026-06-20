using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Common.Exceptions;
using StoneBridge.Application.Supplier.PurchaseOrders.DTOs;

namespace StoneBridge.Application.Supplier.PurchaseOrders.Commands.CancelPo;

public sealed class CancelPoCommandHandler : IRequestHandler<CancelPoCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _repo;
    private readonly ICurrentTenant           _currentTenant;

    public CancelPoCommandHandler(IPurchaseOrderRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public async Task<PurchaseOrderDto> Handle(CancelPoCommand request, CancellationToken cancellationToken)
    {
        var result = await _repo.CancelAsync(
            _currentTenant.TenantId, request.PoId, request.Reason, cancellationToken);

        if (result is null)
        {
            throw new NotFoundException("PurchaseOrder", request.PoId);
        }

        return result;
    }
}
