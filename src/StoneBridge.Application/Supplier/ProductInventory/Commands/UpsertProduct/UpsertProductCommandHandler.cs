using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.ProductInventory.DTOs;

namespace StoneBridge.Application.Supplier.ProductInventory.Commands.UpsertProduct;

public sealed class UpsertProductCommandHandler : IRequestHandler<UpsertProductCommand, ProductInventoryDto>
{
    private readonly IProductInventoryRepository _repo;
    private readonly ICurrentTenant              _currentTenant;

    public UpsertProductCommandHandler(IProductInventoryRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task<ProductInventoryDto> Handle(UpsertProductCommand request, CancellationToken cancellationToken)
        => _repo.UpsertAsync(_currentTenant.TenantId, new UpsertProductRequest(
            request.ProductId,
            request.CategoryCode,
            request.Name,
            request.Brand,
            request.Description,
            request.VariantId,
            request.Sku,
            request.VariantName,
            request.UnitOfMeasure,
            request.BasePrice,
            request.QtyAvailable,
            request.LeadTimeDays
        ), cancellationToken);
}
