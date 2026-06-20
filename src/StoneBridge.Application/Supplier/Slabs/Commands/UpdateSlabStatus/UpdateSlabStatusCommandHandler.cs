using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Common.Exceptions;
using StoneBridge.Application.Supplier.Slabs.DTOs;

namespace StoneBridge.Application.Supplier.Slabs.Commands.UpdateSlabStatus;

public sealed class UpdateSlabStatusCommandHandler : IRequestHandler<UpdateSlabStatusCommand, SupplierSlabDto>
{
    private readonly ISupplierSlabRepository _repo;
    private readonly ICurrentTenant          _currentTenant;

    public UpdateSlabStatusCommandHandler(ISupplierSlabRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public async Task<SupplierSlabDto> Handle(UpdateSlabStatusCommand request, CancellationToken cancellationToken)
    {
        var result = await _repo.UpdateStatusAsync(
            _currentTenant.TenantId, request.SlabId, request.Status, cancellationToken);

        if (result is null)
        {
            throw new NotFoundException("Slab", request.SlabId);
        }

        return result;
    }
}
