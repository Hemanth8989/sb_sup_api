using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Connections.DTOs;

namespace StoneBridge.Application.Supplier.Connections.Commands.UpdateNotes;

public sealed class UpdateNotesCommandHandler : IRequestHandler<UpdateNotesCommand, ConnectionDto>
{
    private readonly IConnectionRepository _repo;
    private readonly ICurrentTenant        _currentTenant;

    public UpdateNotesCommandHandler(IConnectionRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task<ConnectionDto> Handle(UpdateNotesCommand request, CancellationToken cancellationToken)
        => _repo.UpdateNotesAsync(_currentTenant.TenantId, request.ConnectionId, request.Notes, cancellationToken);
}
