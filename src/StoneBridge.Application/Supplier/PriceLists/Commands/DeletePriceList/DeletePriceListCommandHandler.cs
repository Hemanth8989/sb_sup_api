using MediatR;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Application.Supplier.PriceLists.Commands.DeletePriceList;

public sealed class DeletePriceListCommandHandler : IRequestHandler<DeletePriceListCommand>
{
    private readonly IPriceListRepository _repo;
    private readonly ICurrentTenant       _currentTenant;

    public DeletePriceListCommandHandler(IPriceListRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public Task Handle(DeletePriceListCommand request, CancellationToken cancellationToken)
        => _repo.DeleteAsync(_currentTenant.TenantId, request.PriceListId, cancellationToken);
}
