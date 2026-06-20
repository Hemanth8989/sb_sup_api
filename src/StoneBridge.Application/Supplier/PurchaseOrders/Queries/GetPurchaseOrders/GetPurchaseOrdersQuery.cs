using MediatR;
using StoneBridge.Application.Common.Models;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.PurchaseOrders.DTOs;

namespace StoneBridge.Application.Supplier.PurchaseOrders.Queries.GetPurchaseOrders;

public sealed record GetPurchaseOrdersQuery(PoFilterParams Filter) : IRequest<PagedResult<PurchaseOrderDto>>;
