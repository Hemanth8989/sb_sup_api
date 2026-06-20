using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Common.Exceptions;
using StoneBridge.Application.Supplier.PurchaseOrders.DTOs;

namespace StoneBridge.Application.Supplier.PurchaseOrders.Commands.AcknowledgePo;

public sealed class AcknowledgePoCommandHandler : IRequestHandler<AcknowledgePoCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _repo;
    private readonly ICurrentTenant           _currentTenant;

    public AcknowledgePoCommandHandler(IPurchaseOrderRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public async Task<PurchaseOrderDto> Handle(AcknowledgePoCommand request, CancellationToken cancellationToken)
    {
        var result = await _repo.AcknowledgeAsync(
            _currentTenant.TenantId, request.PoId, request.Request, cancellationToken);

        if (result is null)
        {
            throw new NotFoundException("PurchaseOrder", request.PoId);
        }

        return result;
    }
}
