using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Common.Exceptions;
using StoneBridge.Application.Supplier.PurchaseOrders.DTOs;

namespace StoneBridge.Application.Supplier.PurchaseOrders.Commands.UpdatePoNotes;

public sealed class UpdatePoNotesCommandHandler : IRequestHandler<UpdatePoNotesCommand, PurchaseOrderDto>
{
    private readonly IPurchaseOrderRepository _repo;
    private readonly ICurrentTenant           _currentTenant;

    public UpdatePoNotesCommandHandler(IPurchaseOrderRepository repo, ICurrentTenant currentTenant)
    {
        _repo          = repo;
        _currentTenant = currentTenant;
    }

    public async Task<PurchaseOrderDto> Handle(UpdatePoNotesCommand request, CancellationToken cancellationToken)
    {
        var result = await _repo.UpdateSupplierNotesAsync(
            _currentTenant.TenantId, request.PoId, request.Notes, cancellationToken);

        if (result is null)
        {
            throw new NotFoundException("PurchaseOrder", request.PoId);
        }

        return result;
    }
}
