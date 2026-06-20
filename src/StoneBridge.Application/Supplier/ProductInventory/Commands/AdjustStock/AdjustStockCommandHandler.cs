using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Common.Exceptions;
using StoneBridge.Application.Supplier.ProductInventory.DTOs;

namespace StoneBridge.Application.Supplier.ProductInventory.Commands.AdjustStock;

public sealed class AdjustStockCommandHandler : IRequestHandler<AdjustStockCommand, ProductVariantInventoryDto>
{
    private readonly IProductInventoryRepository _repo;
    private readonly ICurrentTenant              _currentTenant;

    public AdjustStockCommandHandler(IProductInventoryRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public async Task<ProductVariantInventoryDto> Handle(
        AdjustStockCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await _repo.AdjustStockAsync(
                _currentTenant.TenantId,
                request.VariantId,
                request.Delta,
                request.NewPrice,
                cancellationToken);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("below zero"))
        {
            throw new BusinessRuleException("InsufficientStock", ex.Message);
        }
    }
}
