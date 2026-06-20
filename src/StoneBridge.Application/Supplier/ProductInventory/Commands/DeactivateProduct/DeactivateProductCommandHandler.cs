using MediatR;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Application.Supplier.ProductInventory.Commands.DeactivateProduct;

public sealed class DeactivateProductCommandHandler : IRequestHandler<DeactivateProductCommand>
{
    private readonly IProductInventoryRepository _repo;
    private readonly ICurrentTenant              _currentTenant;

    public DeactivateProductCommandHandler(IProductInventoryRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task Handle(DeactivateProductCommand request, CancellationToken cancellationToken)
        => _repo.DeactivateAsync(_currentTenant.TenantId, request.ProductId, cancellationToken);
}
