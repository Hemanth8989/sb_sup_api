using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Common.Models;
using StoneBridge.Application.Supplier.PurchaseOrders.DTOs;

namespace StoneBridge.Application.Supplier.PurchaseOrders.Queries.GetPurchaseOrders;

public sealed class GetPurchaseOrdersQueryHandler
    : IRequestHandler<GetPurchaseOrdersQuery, PagedResult<PurchaseOrderDto>>
{
    private readonly IPurchaseOrderRepository _repo;
    private readonly ICurrentTenant           _currentTenant;

    public GetPurchaseOrdersQueryHandler(IPurchaseOrderRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task<PagedResult<PurchaseOrderDto>> Handle(
        GetPurchaseOrdersQuery request, CancellationToken cancellationToken)
        => _repo.GetAllAsync(_currentTenant.TenantId, request.Filter, cancellationToken);
}
