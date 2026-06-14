using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Common.Exceptions;
using StoneBridge.Application.Supplier.Bundles.DTOs;

namespace StoneBridge.Application.Supplier.Bundles.Commands.UpdateBundle;

public sealed class UpdateBundleCommandHandler : IRequestHandler<UpdateBundleCommand, BundleDto>
{
    private readonly IBundleRepository _repo;
    private readonly ICurrentTenant    _currentTenant;

    public UpdateBundleCommandHandler(IBundleRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public async Task<BundleDto> Handle(UpdateBundleCommand request, CancellationToken cancellationToken)
    {
        var result = await _repo.UpdateAsync(
            _currentTenant.TenantId, request.BundleId, request.Request, cancellationToken);

        if (result is null)
        {
            throw new NotFoundException("Bundle", request.BundleId);
        }

        return result;
    }
}
