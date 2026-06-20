using MediatR;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Application.Supplier.Slabs.Commands.BulkUpdateSlabStatus;

public sealed class BulkUpdateSlabStatusCommandHandler : IRequestHandler<BulkUpdateSlabStatusCommand, int>
{
    private readonly ISupplierSlabRepository _repo;
    private readonly ICurrentTenant          _currentTenant;

    public BulkUpdateSlabStatusCommandHandler(ISupplierSlabRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task<int> Handle(BulkUpdateSlabStatusCommand request, CancellationToken cancellationToken)
        => _repo.BulkUpdateStatusAsync(
            _currentTenant.TenantId, request.SlabIds, request.Status, cancellationToken);
}
