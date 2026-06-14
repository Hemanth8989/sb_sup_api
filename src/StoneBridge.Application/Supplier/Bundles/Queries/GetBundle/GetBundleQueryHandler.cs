using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Bundles.DTOs;

namespace StoneBridge.Application.Supplier.Bundles.Queries.GetBundle;

public sealed class GetBundleQueryHandler : IRequestHandler<GetBundleQuery, BundleDetailDto?>
{
    private readonly IBundleRepository _repo;
    private readonly ICurrentTenant    _currentTenant;

    public GetBundleQueryHandler(IBundleRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task<BundleDetailDto?> Handle(
        GetBundleQuery request, CancellationToken cancellationToken)
        => _repo.GetByIdAsync(_currentTenant.TenantId, request.BundleId, cancellationToken);
}
