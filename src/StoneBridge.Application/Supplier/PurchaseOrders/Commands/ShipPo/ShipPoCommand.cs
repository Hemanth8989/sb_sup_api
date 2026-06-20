using MediatR;
using StoneBridge.Application.Supplier.PurchaseOrders.DTOs;

namespace StoneBridge.Application.Supplier.PurchaseOrders.Commands.ShipPo;

public sealed record ShipPoCommand(Guid PoId, ShipPoRequest Request) : IRequest<PurchaseOrderDto>;
