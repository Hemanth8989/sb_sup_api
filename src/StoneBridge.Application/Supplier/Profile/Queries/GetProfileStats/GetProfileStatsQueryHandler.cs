using MediatR;
using StoneBridge.Application.Common.Exceptions;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Profile.DTOs;

namespace StoneBridge.Application.Supplier.Profile.Queries.GetProfileStats;

public sealed class GetProfileStatsQueryHandler : IRequestHandler<GetProfileStatsQuery, SupplierStatsDto>
{
    private readonly ISupplierProfileRepository _repository;
    private readonly ICurrentTenant             _currentTenant;

    public GetProfileStatsQueryHandler(ISupplierProfileRepository repository, ICurrentTenant currentTenant)
    {
        _repository    = repository;
        _currentTenant = currentTenant;
    }

    public async Task<SupplierStatsDto> Handle(GetProfileStatsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentTenant.IsSupplier)
        {
            throw new ForbiddenException("Profile stats are only available to supplier tenants.");
        }

        return await _repository.GetStatsAsync(_currentTenant.TenantId, cancellationToken)
            ?? new SupplierStatsDto();
    }
}
