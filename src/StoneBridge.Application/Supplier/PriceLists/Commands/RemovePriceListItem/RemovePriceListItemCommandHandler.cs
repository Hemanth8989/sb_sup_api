using MediatR;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Application.Supplier.PriceLists.Commands.RemovePriceListItem;

public sealed class RemovePriceListItemCommandHandler : IRequestHandler<RemovePriceListItemCommand>
{
    private readonly IPriceListRepository _repo;
    private readonly ICurrentTenant       _currentTenant;

    public RemovePriceListItemCommandHandler(IPriceListRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task Handle(RemovePriceListItemCommand request, CancellationToken cancellationToken)
        => _repo.RemoveItemAsync(_currentTenant.TenantId, request.PriceListId, request.ItemId, cancellationToken);
}
