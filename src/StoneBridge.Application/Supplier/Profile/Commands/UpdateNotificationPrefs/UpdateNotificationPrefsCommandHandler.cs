using System.Text.Json.Nodes;
using MediatR;
using StoneBridge.Application.Common.Exceptions;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Application.Supplier.Profile.Commands.UpdateNotificationPrefs;

public sealed class UpdateNotificationPrefsCommandHandler : IRequestHandler<UpdateNotificationPrefsCommand, JsonObject>
{
    private readonly ISupplierProfileRepository _repository;
    private readonly ICurrentTenant             _currentTenant;

    public UpdateNotificationPrefsCommandHandler(ISupplierProfileRepository repository, ICurrentTenant currentTenant)
    {
        _repository    = repository;
        _currentTenant = currentTenant;
    }

    public async Task<JsonObject> Handle(UpdateNotificationPrefsCommand request, CancellationToken cancellationToken)
    {
        if (!_currentTenant.IsSupplier)
        {
            throw new ForbiddenException("Notification preferences are only available to supplier tenants.");
        }

        return await _repository.UpdateNotificationPrefsAsync(
            _currentTenant.TenantId,
            request.Prefs,
            cancellationToken);
    }
}
