using MediatR;
using StoneBridge.Application.Supplier.PurchaseOrders.DTOs;

namespace StoneBridge.Application.Supplier.PurchaseOrders.Commands.UpdatePoStatus;

public sealed record UpdatePoStatusCommand(Guid PoId, UpdatePoStatusRequest Request) : IRequest<PurchaseOrderDto>;
