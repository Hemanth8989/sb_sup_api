using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Connections.DTOs;

namespace StoneBridge.Application.Supplier.Connections.Queries.GetConnection;

public sealed class GetConnectionQueryHandler : IRequestHandler<GetConnectionQuery, ConnectionDto?>
{
    private readonly IConnectionRepository _repo;
    private readonly ICurrentTenant        _currentTenant;

    public GetConnectionQueryHandler(IConnectionRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task<ConnectionDto?> Handle(GetConnectionQuery request, CancellationToken cancellationToken)
        => _repo.GetByIdAsync(_currentTenant.TenantId, request.ConnectionId, cancellationToken);
}
