using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Common.Models;
using StoneBridge.Application.Supplier.ProductInventory.DTOs;

namespace StoneBridge.Application.Supplier.ProductInventory.Queries.GetProducts;

public sealed class GetProductsQueryHandler
    : IRequestHandler<GetProductsQuery, PagedResult<ProductInventoryDto>>
{
    private readonly IProductInventoryRepository _repo;
    private readonly ICurrentTenant              _currentTenant;

    public GetProductsQueryHandler(IProductInventoryRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task<PagedResult<ProductInventoryDto>> Handle(
        GetProductsQuery request, CancellationToken cancellationToken)
        => _repo.GetAllAsync(_currentTenant.TenantId, request.Filter, cancellationToken);
}
