using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Common.Exceptions;
using StoneBridge.Application.Supplier.PriceLists.DTOs;

namespace StoneBridge.Application.Supplier.PriceLists.Commands.UpdatePriceList;

public sealed class UpdatePriceListCommandHandler : IRequestHandler<UpdatePriceListCommand, PriceListDto>
{
    private readonly IPriceListRepository _repo;
    private readonly ICurrentTenant       _currentTenant;

    public UpdatePriceListCommandHandler(IPriceListRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public async Task<PriceListDto> Handle(UpdatePriceListCommand request, CancellationToken cancellationToken)
    {
        var result = await _repo.UpdateAsync(
            _currentTenant.TenantId,
            request.PriceListId,
            new UpdatePriceListRequest(request.Name, request.Tier, request.Currency, null, null, request.IsActive),
            cancellationToken);

        if (result is null)
        {
            throw new NotFoundException("PriceList", request.PriceListId);
        }

        return result;
    }
}
