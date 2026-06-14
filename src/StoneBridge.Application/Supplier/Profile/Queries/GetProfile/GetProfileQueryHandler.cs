using MediatR;
using StoneBridge.Application.Common.Exceptions;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Profile.DTOs;

namespace StoneBridge.Application.Supplier.Profile.Queries.GetProfile;

public sealed class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, SupplierProfileDto>
{
    private readonly ISupplierProfileRepository _repository;
    private readonly ICurrentTenant             _currentTenant;

    public GetProfileQueryHandler(ISupplierProfileRepository repository, ICurrentTenant currentTenant)
    {
        _repository    = repository;
        _currentTenant = currentTenant;
    }

    public async Task<SupplierProfileDto> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        if (!_currentTenant.IsSupplier)
        {
            throw new ForbiddenException("Profile management is only available to supplier tenants.");
        }

        var profile = await _repository.GetProfileAsync(_currentTenant.TenantId, cancellationToken);

        if (profile is null)
        {
            // Auto-create a minimal profile row if none exists (new tenant)
            profile = await _repository.UpsertProfileAsync(
                _currentTenant.TenantId,
                new UpdateProfileRequest { DisplayName = "My Supplier Company" },
                cancellationToken);
        }

        return profile;
    }
}
