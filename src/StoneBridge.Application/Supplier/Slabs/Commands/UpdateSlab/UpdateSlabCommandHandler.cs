using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Common.Exceptions;
using StoneBridge.Application.Supplier.Slabs.DTOs;

namespace StoneBridge.Application.Supplier.Slabs.Commands.UpdateSlab;

public sealed class UpdateSlabCommandHandler : IRequestHandler<UpdateSlabCommand, SupplierSlabDto>
{
    private readonly ISupplierSlabRepository _repo;
    private readonly ICurrentTenant          _currentTenant;

    public UpdateSlabCommandHandler(ISupplierSlabRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public async Task<SupplierSlabDto> Handle(UpdateSlabCommand request, CancellationToken cancellationToken)
    {
        var result = await _repo.UpdateAsync(
            _currentTenant.TenantId, request.SlabId, request.Request, cancellationToken);

        if (result is null)
        {
            throw new NotFoundException("Slab", request.SlabId);
        }

        return result;
    }
}
