using MediatR;

namespace StoneBridge.Application.Supplier.Connections.Commands.AssignPriceList;

public sealed record AssignPriceListCommand(Guid ConnectionId, Guid? PriceListId) : IRequest;
