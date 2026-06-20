using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Common.Exceptions;
using StoneBridge.Application.Supplier.Connections.DTOs;

namespace StoneBridge.Application.Supplier.Connections.Commands.UpdateConnectionTier;

public sealed class UpdateConnectionTierCommandHandler : IRequestHandler<UpdateConnectionTierCommand, ConnectionDto>
{
    private readonly IConnectionRepository _repo;
    private readonly ICurrentTenant        _currentTenant;

    public UpdateConnectionTierCommandHandler(IConnectionRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public async Task<ConnectionDto> Handle(UpdateConnectionTierCommand request, CancellationToken cancellationToken)
    {
        var result = await _repo.UpdateTierAsync(
            _currentTenant.TenantId, request.ConnectionId, request.Tier, cancellationToken);

        if (result is null)
        {
            throw new NotFoundException("Connection", request.ConnectionId);
        }

        return result;
    }
}
