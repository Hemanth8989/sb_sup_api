using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Common.Exceptions;
using StoneBridge.Application.Supplier.Slabs.DTOs;

namespace StoneBridge.Application.Supplier.Slabs.Commands.SetSlabPrice;

public sealed class SetSlabPriceCommandHandler : IRequestHandler<SetSlabPriceCommand, SupplierSlabDto>
{
    private readonly ISupplierSlabRepository _repo;
    private readonly ICurrentTenant          _currentTenant;

    public SetSlabPriceCommandHandler(ISupplierSlabRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public async Task<SupplierSlabDto> Handle(SetSlabPriceCommand request, CancellationToken cancellationToken)
    {
        var result = await _repo.SetPriceOverrideAsync(
            _currentTenant.TenantId, request.SlabId, request.PriceOverride, cancellationToken);

        if (result is null)
        {
            throw new NotFoundException("Slab", request.SlabId);
        }

        return result;
    }
}
