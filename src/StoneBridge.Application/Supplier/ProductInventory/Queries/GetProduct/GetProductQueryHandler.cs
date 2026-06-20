using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.ProductInventory.DTOs;

namespace StoneBridge.Application.Supplier.ProductInventory.Queries.GetProduct;

public sealed class GetProductQueryHandler : IRequestHandler<GetProductQuery, ProductInventoryDto?>
{
    private readonly IProductInventoryRepository _repo;
    private readonly ICurrentTenant              _currentTenant;

    public GetProductQueryHandler(IProductInventoryRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task<ProductInventoryDto?> Handle(GetProductQuery request, CancellationToken cancellationToken)
        => _repo.GetByIdAsync(_currentTenant.TenantId, request.ProductId, cancellationToken);
}
