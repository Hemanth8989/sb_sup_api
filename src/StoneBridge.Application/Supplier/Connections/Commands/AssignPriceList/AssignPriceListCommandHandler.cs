using MediatR;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Application.Supplier.Connections.Commands.AssignPriceList;

public sealed class AssignPriceListCommandHandler : IRequestHandler<AssignPriceListCommand>
{
    private readonly IConnectionRepository _repo;
    private readonly ICurrentTenant        _currentTenant;

    public AssignPriceListCommandHandler(IConnectionRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public async Task Handle(AssignPriceListCommand request, CancellationToken cancellationToken)
    {
        if (request.PriceListId.HasValue)
        {
            await _repo.AssignPriceListAsync(
                _currentTenant.TenantId, request.ConnectionId,
                request.PriceListId.Value, _currentTenant.UserId, cancellationToken);
        }
        else
        {
            await _repo.RemovePriceListAsync(_currentTenant.TenantId, request.ConnectionId, cancellationToken);
        }
    }
}
