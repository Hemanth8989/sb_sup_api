using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.PriceLists.DTOs;

namespace StoneBridge.Application.Supplier.PriceLists.Queries.GetPriceLists;

public sealed class GetPriceListsQueryHandler : IRequestHandler<GetPriceListsQuery, IReadOnlyList<PriceListDto>>
{
    private readonly IPriceListRepository _repo;
    private readonly ICurrentTenant       _currentTenant;

    public GetPriceListsQueryHandler(IPriceListRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task<IReadOnlyList<PriceListDto>> Handle(GetPriceListsQuery request, CancellationToken cancellationToken)
        => _repo.GetAllAsync(_currentTenant.TenantId, cancellationToken);
}
