using MediatR;
using StoneBridge.Application.Supplier.PurchaseOrders.DTOs;

namespace StoneBridge.Application.Supplier.PurchaseOrders.Commands.UpdatePoNotes;

public sealed record UpdatePoNotesCommand(Guid PoId, string? Notes) : IRequest<PurchaseOrderDto>;
