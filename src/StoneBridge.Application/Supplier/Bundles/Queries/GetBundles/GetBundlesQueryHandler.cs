using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Bundles.DTOs;

namespace StoneBridge.Application.Supplier.Bundles.Queries.GetBundles;

public sealed class GetBundlesQueryHandler
    : IRequestHandler<GetBundlesQuery, IReadOnlyList<BundleDto>>
{
    private readonly IBundleRepository _repo;
    private readonly ICurrentTenant    _currentTenant;

    public GetBundlesQueryHandler(IBundleRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task<IReadOnlyList<BundleDto>> Handle(
        GetBundlesQuery request, CancellationToken cancellationToken)
        => _repo.GetAllAsync(_currentTenant.TenantId, request.Search, cancellationToken);
}
