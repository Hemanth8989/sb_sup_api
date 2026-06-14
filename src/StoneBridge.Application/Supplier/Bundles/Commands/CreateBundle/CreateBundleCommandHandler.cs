using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Bundles.DTOs;

namespace StoneBridge.Application.Supplier.Bundles.Commands.CreateBundle;

public sealed class CreateBundleCommandHandler : IRequestHandler<CreateBundleCommand, BundleDto>
{
    private readonly IBundleRepository _repo;
    private readonly ICurrentTenant    _currentTenant;

    public CreateBundleCommandHandler(IBundleRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task<BundleDto> Handle(CreateBundleCommand request, CancellationToken cancellationToken)
        => _repo.CreateAsync(_currentTenant.TenantId, request.Request, cancellationToken);
}
