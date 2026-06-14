using System.Text.Json.Nodes;
using MediatR;
using StoneBridge.Application.Common.Exceptions;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Application.Supplier.Profile.Queries.GetNotificationPrefs;

public sealed class GetNotificationPrefsQueryHandler : IRequestHandler<GetNotificationPrefsQuery, JsonObject>
{
    private readonly ISupplierProfileRepository _repository;
    private readonly ICurrentTenant             _currentTenant;

    public GetNotificationPrefsQueryHandler(ISupplierProfileRepository repository, ICurrentTenant currentTenant)
    {
        _repository    = repository;
        _currentTenant = currentTenant;
    }

    public async Task<JsonObject> Handle(GetNotificationPrefsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentTenant.IsSupplier)
        {
            throw new ForbiddenException("Notification preferences are only available to supplier tenants.");
        }

        return await _repository.GetNotificationPrefsAsync(_currentTenant.TenantId, cancellationToken);
    }
}
