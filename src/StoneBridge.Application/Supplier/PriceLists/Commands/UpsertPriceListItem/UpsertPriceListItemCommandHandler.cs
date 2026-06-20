using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.PriceLists.DTOs;

namespace StoneBridge.Application.Supplier.PriceLists.Commands.UpsertPriceListItem;

public sealed class UpsertPriceListItemCommandHandler : IRequestHandler<UpsertPriceListItemCommand, PriceListItemDto>
{
    private readonly IPriceListRepository _repo;
    private readonly ICurrentTenant       _currentTenant;

    public UpsertPriceListItemCommandHandler(IPriceListRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task<PriceListItemDto> Handle(UpsertPriceListItemCommand request, CancellationToken cancellationToken)
        => _repo.UpsertItemAsync(
            _currentTenant.TenantId,
            request.PriceListId,
            new UpsertPriceListItemRequest(request.VariantId, request.UnitPrice),
            cancellationToken);
}
