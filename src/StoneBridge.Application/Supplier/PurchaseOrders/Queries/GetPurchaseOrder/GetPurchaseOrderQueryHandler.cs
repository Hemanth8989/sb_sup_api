using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.PurchaseOrders.DTOs;

namespace StoneBridge.Application.Supplier.PurchaseOrders.Queries.GetPurchaseOrder;

public sealed class GetPurchaseOrderQueryHandler
    : IRequestHandler<GetPurchaseOrderQuery, PurchaseOrderDto?>
{
    private readonly IPurchaseOrderRepository _repo;
    private readonly ICurrentTenant           _currentTenant;

    public GetPurchaseOrderQueryHandler(IPurchaseOrderRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task<PurchaseOrderDto?> Handle(
        GetPurchaseOrderQuery request, CancellationToken cancellationToken)
        => _repo.GetByIdAsync(_currentTenant.TenantId, request.PoId, cancellationToken);
}
