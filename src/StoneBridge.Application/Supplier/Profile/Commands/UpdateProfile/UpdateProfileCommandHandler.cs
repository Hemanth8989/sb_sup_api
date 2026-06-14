using MediatR;
using StoneBridge.Application.Common.Exceptions;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Profile.DTOs;

namespace StoneBridge.Application.Supplier.Profile.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, SupplierProfileDto>
{
    private readonly ISupplierProfileRepository _repository;
    private readonly ICurrentTenant             _currentTenant;

    public UpdateProfileCommandHandler(ISupplierProfileRepository repository, ICurrentTenant currentTenant)
    {
        _repository    = repository;
        _currentTenant = currentTenant;
    }

    public async Task<SupplierProfileDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        if (!_currentTenant.IsSupplier)
        {
            throw new ForbiddenException("Only supplier tenants can update supplier profiles.");
        }

        return await _repository.UpsertProfileAsync(
            _currentTenant.TenantId,
            request.Request,
            cancellationToken);
    }
}
