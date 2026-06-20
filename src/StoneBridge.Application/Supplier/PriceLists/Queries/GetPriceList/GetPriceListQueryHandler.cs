using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.PriceLists.DTOs;

namespace StoneBridge.Application.Supplier.PriceLists.Queries.GetPriceList;

public sealed class GetPriceListQueryHandler : IRequestHandler<GetPriceListQuery, PriceListDetailDto?>
{
    private readonly IPriceListRepository _repo;
    private readonly ICurrentTenant       _currentTenant;

    public GetPriceListQueryHandler(IPriceListRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task<PriceListDetailDto?> Handle(GetPriceListQuery request, CancellationToken cancellationToken)
        => _repo.GetByIdAsync(_currentTenant.TenantId, request.PriceListId, cancellationToken);
}
