using MediatR;
using StoneBridge.Application.Supplier.PurchaseOrders.DTOs;

namespace StoneBridge.Application.Supplier.PurchaseOrders.Queries.GetPurchaseOrder;

public sealed record GetPurchaseOrderQuery(Guid PoId) : IRequest<PurchaseOrderDto?>;
