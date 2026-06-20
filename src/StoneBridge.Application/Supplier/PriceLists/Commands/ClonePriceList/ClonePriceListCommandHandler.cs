using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.PriceLists.DTOs;

namespace StoneBridge.Application.Supplier.PriceLists.Commands.ClonePriceList;

public sealed class ClonePriceListCommandHandler : IRequestHandler<ClonePriceListCommand, PriceListDto>
{
    private readonly IPriceListRepository _repo;
    private readonly ICurrentTenant       _currentTenant;

    public ClonePriceListCommandHandler(IPriceListRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task<PriceListDto> Handle(ClonePriceListCommand request, CancellationToken cancellationToken)
        => _repo.CloneAsync(_currentTenant.TenantId, request.SourceId, request.NewName, cancellationToken);
}
