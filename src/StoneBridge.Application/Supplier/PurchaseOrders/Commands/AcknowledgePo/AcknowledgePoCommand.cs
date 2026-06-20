using MediatR;
using StoneBridge.Application.Supplier.PurchaseOrders.DTOs;

namespace StoneBridge.Application.Supplier.PurchaseOrders.Commands.AcknowledgePo;

public sealed record AcknowledgePoCommand(Guid PoId, AcknowledgePoRequest Request) : IRequest<PurchaseOrderDto>;
