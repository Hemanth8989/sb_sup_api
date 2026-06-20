using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Common.Exceptions;

namespace StoneBridge.Application.Supplier.Slabs.Commands.DeleteSlab;

public sealed class DeleteSlabCommandHandler : IRequestHandler<DeleteSlabCommand>
{
    private readonly ISupplierSlabRepository _repo;
    private readonly ICurrentTenant          _currentTenant;

    public DeleteSlabCommandHandler(ISupplierSlabRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public async Task Handle(DeleteSlabCommand request, CancellationToken cancellationToken)
    {
        var deleted = await _repo.DeleteAsync(
            _currentTenant.TenantId, request.SlabId, cancellationToken);

        if (!deleted)
        {
            throw new NotFoundException("Slab", request.SlabId);
        }
    }
}
