using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Connections.DTOs;

namespace StoneBridge.Application.Supplier.Connections.Queries.GetConnections;

public sealed class GetConnectionsQueryHandler
    : IRequestHandler<GetConnectionsQuery, IReadOnlyList<ConnectionDto>>
{
    private readonly IConnectionRepository _repo;
    private readonly ICurrentTenant        _currentTenant;

    public GetConnectionsQueryHandler(IConnectionRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task<IReadOnlyList<ConnectionDto>> Handle(
        GetConnectionsQuery request, CancellationToken cancellationToken)
        => _repo.GetAllAsync(_currentTenant.TenantId, request.Status, cancellationToken);
}
