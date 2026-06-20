using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.PriceLists.DTOs;

namespace StoneBridge.Application.Supplier.PriceLists.Commands.CreatePriceList;

public sealed class CreatePriceListCommandHandler : IRequestHandler<CreatePriceListCommand, PriceListDto>
{
    private readonly IPriceListRepository _repo;
    private readonly ICurrentTenant       _currentTenant;

    public CreatePriceListCommandHandler(IPriceListRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task<PriceListDto> Handle(CreatePriceListCommand request, CancellationToken cancellationToken)
        => _repo.CreateAsync(
            _currentTenant.TenantId,
            new CreatePriceListRequest(request.Name, request.Tier, request.Currency, null, null),
            cancellationToken);
}
