using MediatR;
using StoneBridge.Application.Supplier.PurchaseOrders.DTOs;

namespace StoneBridge.Application.Supplier.PurchaseOrders.Commands.CancelPo;

public sealed record CancelPoCommand(Guid PoId, string? Reason) : IRequest<PurchaseOrderDto>;
