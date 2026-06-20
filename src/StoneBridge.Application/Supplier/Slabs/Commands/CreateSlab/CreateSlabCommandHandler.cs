using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Slabs.DTOs;

namespace StoneBridge.Application.Supplier.Slabs.Commands.CreateSlab;

public sealed class CreateSlabCommandHandler : IRequestHandler<CreateSlabCommand, SupplierSlabDto>
{
    private readonly ISupplierSlabRepository _repo;
    private readonly ICurrentTenant          _currentTenant;

    public CreateSlabCommandHandler(ISupplierSlabRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task<SupplierSlabDto> Handle(CreateSlabCommand request, CancellationToken cancellationToken)
        => _repo.CreateAsync(_currentTenant.TenantId, request.Request, cancellationToken);
}
