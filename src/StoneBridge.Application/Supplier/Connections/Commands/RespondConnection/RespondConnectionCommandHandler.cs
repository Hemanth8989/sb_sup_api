using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Common.Exceptions;
using StoneBridge.Application.Supplier.Connections.DTOs;

namespace StoneBridge.Application.Supplier.Connections.Commands.RespondConnection;

public sealed class RespondConnectionCommandHandler : IRequestHandler<RespondConnectionCommand, ConnectionDto>
{
    private readonly IConnectionRepository _repo;
    private readonly ICurrentTenant        _currentTenant;

    public RespondConnectionCommandHandler(IConnectionRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public async Task<ConnectionDto> Handle(RespondConnectionCommand request, CancellationToken cancellationToken)
    {
        var validActions = new[] { "approve", "decline", "suspend", "terminate", "reactivate" };
        if (!validActions.Contains(request.Action))
        {
            throw new BusinessRuleException("InvalidAction", $"Invalid action '{request.Action}'. Valid: {string.Join(", ", validActions)}");
        }

        var result = await _repo.RespondAsync(
            _currentTenant.TenantId, request.ConnectionId, request.Action, request.Reason, cancellationToken);

        if (result is null)
        {
            throw new NotFoundException("Connection", request.ConnectionId);
        }

        return result;
    }
}
